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
		Living CurrentLiving { get { return m_livingList[m_currentLivingIndex]; } }
		void ResetLivingIndex() { m_currentLivingIndex = 0; }
		bool MoveToNextLiving()
		{
			Debug.Assert(m_currentLivingIndex < m_livingList.Count);
			++m_currentLivingIndex;
			return m_currentLivingIndex < m_livingList.Count;
		}

		void InitializeWorldTick()
		{
			m_minTickTimer = new Timer(this.MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this.MaxMoveTimerCallback);
		}

		void MinTickTimerCallback(object stateInfo)
		{
			VDbg("MinTickTimerCallback");
			m_minTickTimerTriggered = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		void MaxMoveTimerCallback(object stateInfo)
		{
			VDbg("MaxMoveTimerCallback");
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

			if (m_config.RequireUser && !this.HasUsers)
				return false;

			if (m_config.RequireControllables && !m_userList.Any(u => u.Controllables.Count > 0))
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
			//MyDebug.WriteLine("-- Pretick {0} events --", m_tickNumber + 1);

			m_preTickInvokeList.ProcessInvokeList();
			ProcessAddLivingList();
			ProcessRemoveLivingList();

			//MyDebug.WriteLine("-- Pretick {0} events done --", m_tickNumber + 1);
		}

		bool WorkAvailable()
		{
			VerifyAccess();

			if (m_instantInvokeList.HasInvokeWork)
			{
				VDbg("WorkAvailable: InstantInvoke");
				return true;
			}

			if (m_state == WorldState.Idle)
			{
				if (m_preTickInvokeList.HasInvokeWork)
				{
					VDbg("WorkAvailable: PreTickInvoke");
					return true;
				}

				if (this.HasAddLivings)
				{
					VDbg("WorkAvailable: AddLiving");
					return true;
				}

				if (this.HasRemoveLivings)
				{
					VDbg("WorkAvailable: RemoveLiving");
					return true;
				}

				if (IsTimeToStartTick())
				{
					VDbg("WorkAvailable: IsTimeToStartTick");
					return true;
				}

				return false;
			}
			else if (m_state == WorldState.TickOngoing)
			{
				if (m_config.TickMethod == WorldTickMethod.Simultaneous)
					return SimultaneousWorkAvailable();
				else if (m_config.TickMethod == WorldTickMethod.Sequential)
					return SequentialWorkAvailable();
				else
					throw new NotImplementedException();
			}
			else
			{
				throw new Exception();
			}
		}

		bool SimultaneousWorkAvailable()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			if (m_userList.All(u => u.ProceedTurnReceived))
				return true;

			if (this.UseMaxMoveTime && m_maxMoveTimerTriggered)
				return true;

			return false;
		}

		void SimultaneousWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);
			Debug.Assert(m_userList.All(u => u.StartTurnSent));

			VDbg("SimultaneousWork");

			bool forceMove = IsMoveForced();

			if (!forceMove && !m_userList.All(u => u.ProceedTurnReceived))
				return;

			foreach (var living in m_livingList)
				living.TurnPreRun();

			foreach (var living in m_livingList.Where(l => l.HasAction))
				living.PerformAction();

			EndTurn();

			m_state = WorldState.TickDone;

			VDbg("SimultaneousWork Done");
		}



		bool SequentialWorkAvailable()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			if (RemoveLivingListContains(this.CurrentLiving))
			{
				VDbg("WorkAvailable: Current living is to be removed");
				return true;
			}

			if (this.CurrentLiving.HasAction)
			{
				VDbg("WorkAvailable: Living.HasAction");
				return true;
			}

			if (this.UseMaxMoveTime && m_maxMoveTimerTriggered)
			{
				VDbg("WorkAvailable: NextMoveTime");
				return true;
			}

			return false;
		}

		void SequentialWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			bool forceMove = IsMoveForced();

			VDbg("SequentialWork");

			while (true)
			{
				if (m_livingList.Count == 0)
				{
					VDbg("no livings to handled");
					m_state = WorldState.TickDone;
					break;
				}

				var living = this.CurrentLiving;

				if (RemoveLivingListContains(living))
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
					VDbg("last living handled");
					m_state = WorldState.TickDone;
					break;
				}
			}

			VDbg("SequentialWork Done");
		}

		void StartTick()
		{
			VerifyAccess();

			this.TickNumber++;
			AddChange(new TickStartChange(this.TickNumber));

			Debug.Print("-- Tick {0} started --", this.TickNumber);

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
			foreach (var living in m_livingList)
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

			Debug.Print("-- Tick {0} ended --", this.TickNumber);
			m_state = WorldState.TickEnded;
		}
	}
}
