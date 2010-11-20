using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		public event Action TickStartEvent;
		public int TickNumber { get; private set; }

		enum WorldState
		{
			Idle,
			TickOngoing,
			TickDone,
			TickEnded,
		}

		WorldState m_state = WorldState.Idle;

		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }
		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }

		/// <summary>
		/// Timer is used to start the tick after MinTickTime
		/// </summary>
		Timer m_minTickTimer;
		bool m_minTickTimerTriggered = true; // initialize to true to trigger the first tick

		/// <summary>
		/// Timer is used to timeout player turn after MaxMoveTime
		/// </summary>
		Timer m_maxMoveTimer;
		bool m_maxMoveTimerTriggered;

		int m_currentLivingIndex;
		Living CurrentLiving { get { return m_livings.List[m_currentLivingIndex]; } }
		void ResetLivingIndex() { m_currentLivingIndex = 0; }
		bool MoveToNextLiving()
		{
			Debug.Assert(m_currentLivingIndex < m_livings.List.Count);
			++m_currentLivingIndex;
			return m_currentLivingIndex < m_livings.List.Count;
		}

		void InitializeWorldTick()
		{
			m_minTickTimer = new Timer(this.MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this.MaxMoveTimerCallback);
		}

		void MinTickTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MinTickTimerCallback");
			m_minTickTimerTriggered = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		void MaxMoveTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MaxMoveTimerCallback");
			m_maxMoveTimerTriggered = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}


		bool IsTimeToStartTick()
		{
			VerifyAccess();

			if (m_state != WorldState.Idle)
				return false;

			if (this.UseMinTickTime && !m_minTickTimerTriggered)
				return false;

			if (m_config.RequireUser && m_users.List.Count == 0)
				return false;

			if (m_config.RequireControllables && !m_users.List.Any(u => u.Controllables.Count > 0))
				return false;

			return true;
		}

		bool IsMoveForced()
		{
			return this.UseMaxMoveTime && m_maxMoveTimerTriggered;
		}

		void Work()
		{
			VerifyAccess();

			EnterWriteLock();

			m_instantInvokeList.ProcessInvokeList();

			ProcessConnectionAdds();

			foreach (var conn in m_connections.List)
				conn.HandleNewMessages();

			ProcessConnectionRemoves();

			if (m_state == WorldState.Idle)
			{
				PreTickWork();

				if (IsTimeToStartTick())
					StartTick();
			}

			if (m_state == WorldState.TickOngoing)
			{
				if (m_config.TickMethod == WorldTickMethod.Simultaneous)
					SimultaneousWork();
				else if (m_config.TickMethod == WorldTickMethod.Sequential)
					SequentialWork();
				else
					throw new NotImplementedException();
			}

			if (m_state == WorldState.TickDone)
				EndTick();

			ExitWriteLock();

			// no point in entering read lock here, as this thread is the only one that can get a write lock
			if (WorkEnded != null)
				WorkEnded();

			if (m_state == WorldState.TickEnded)
				m_state = WorldState.Idle;
		}

		void PreTickWork()
		{
			m_preTickInvokeList.ProcessInvokeList();
			m_users.Process();
			m_livings.Process();
		}

		void SimultaneousWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);
			Debug.Assert(m_users.List.All(u => u.StartTurnSent));

			trace.TraceVerbose("SimultaneousWork");

			bool forceMove = IsMoveForced();

			if (!forceMove && !m_users.List.All(u => u.ProceedTurnReceived))
				return;

			foreach (var living in m_livings.List)
				living.TurnPreRun();

			foreach (var living in m_livings.List.Where(l => l.HasAction))
				living.PerformAction();

			EndTurn();

			m_state = WorldState.TickDone;

			trace.TraceVerbose("SimultaneousWork Done");
		}


		void SequentialWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			bool forceMove = IsMoveForced();

			trace.TraceVerbose("SequentialWork");

			while (true)
			{
				if (m_livings.List.Count == 0)
				{
					trace.TraceVerbose("no livings to handled");
					m_state = WorldState.TickDone;
					break;
				}

				var living = this.CurrentLiving;

				if (m_livings.RemoveList.Contains(living))
					forceMove = true;

				if (!forceMove && !living.HasAction)
					break;

				living.TurnPreRun();

				living.PerformAction();

				EndTurn(living);

				bool ok = MoveToNextLiving();
				if (ok)
				{
					StartTurnSequential(this.CurrentLiving);
				}
				else
				{
					trace.TraceVerbose("last living handled");
					m_state = WorldState.TickDone;
					break;
				}
			}

			trace.TraceVerbose("SequentialWork Done");
		}

		void StartTick()
		{
			VerifyAccess();

			this.TickNumber++;
			AddChange(new TickStartChange(this.TickNumber));

			trace.TraceInformation("-- Tick {0} started --", this.TickNumber);

			m_state = WorldState.TickOngoing;

			if (TickStartEvent != null)
				TickStartEvent();

			if (m_config.TickMethod == WorldTickMethod.Simultaneous)
			{
				StartTurnSimultaneous();
			}
			else if (m_config.TickMethod == WorldTickMethod.Sequential)
			{
				ResetLivingIndex();

				bool ok = MoveToNextLiving();
				if (ok)
					StartTurnSequential(this.CurrentLiving);
			}
		}

		void StartTurnSimultaneous()
		{
			foreach (var living in m_livings.List)
				living.TurnStarted();

			AddChange(new TurnStartChange());

			if (this.UseMaxMoveTime)
			{
				m_maxMoveTimerTriggered = false;
				Thread.MemoryBarrier();
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));
			}
		}

		void StartTurnSequential(Living living)
		{
			living.TurnStarted();

			AddChange(new TurnStartChange(living));

			if (this.UseMaxMoveTime)
			{
				m_maxMoveTimerTriggered = false;
				Thread.MemoryBarrier();
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));
			}
		}

		void EndTurn(Living living = null)
		{
			AddChange(new TurnEndChange(living));
		}

		void EndTick()
		{
			VerifyAccess();

			if (this.UseMinTickTime)
			{
				m_minTickTimerTriggered = false;
				Thread.MemoryBarrier();
				m_minTickTimer.Change(m_config.MinTickTime, TimeSpan.FromMilliseconds(-1));
			}

			trace.TraceInformation("-- Tick {0} ended --", this.TickNumber);
			m_state = WorldState.TickEnded;
		}
	}
}
