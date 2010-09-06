using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame.Server
{
	public partial class World
	{
		public event Action TickEvent;
		public int TickNumber { get; private set; }

		enum WorldState
		{
			Idle,
			TickOngoing,
			TickDone,
			TickEnded,
		}

		WorldState m_state = WorldState.Idle;

		/// <summary>
		/// time when next move has to happen
		/// </summary>
		DateTime m_nextMove = DateTime.MaxValue;

		/// <summary>
		/// time when next tick will happen
		/// </summary>
		DateTime m_nextTick = DateTime.MinValue;

		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }
		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }

		/// <summary>
		/// If the user has requested to proceed
		/// </summary>
		bool m_tickRequested;

		/// <summary>
		/// Timer is used out-of-tick to start the tick after m_minTickTime and inside-tick to timeout player movements after m_maxMoveTime
		/// </summary>
		Timer m_tickTimer;

		List<Living>.Enumerator m_livingEnumerator;

		void InitializeWorldTick()
		{
			m_tickTimer = new Timer(this.TickTimerCallback);
		}

		void TickTimerCallback(object stateInfo)
		{
			VDbg("TickTimerCallback");
			SignalWorld();
		}


		internal void RequestTick()
		{
			m_tickRequested = true;
			SignalWorld();
		}

		bool IsTimeToStartTick()
		{
			VerifyAccess();

			if (m_state != WorldState.Idle)
				return false;

			if (this.UseMinTickTime && DateTime.Now < m_nextTick)
				return false;

			if (m_config.RequireUser && m_tickRequested == false)
			{
				if (!this.HasUsers)
					return false;
			}

			if (m_config.RequireTickRequest && m_tickRequested == false)
				return false;

			return true;
		}

		bool IsMoveForced()
		{
			return (this.UseMaxMoveTime && DateTime.Now >= m_nextMove) || m_tickRequested;
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

			if (m_livingList.All(l => l.HasAction))
				return true;

			if (this.UseMaxMoveTime && DateTime.Now >= m_nextMove)
				return true;

			return false;
		}

		void SimultaneousWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			bool forceMove = IsMoveForced();

			VDbg("SimultaneousWork");

			if (!forceMove && !m_livingList.All(l => l.HasAction))
				return;

			if (!forceMove)
				Debug.Assert(m_livingList.All(l => l.HasAction));

			foreach (var living in m_livingList)
				living.AI.ActionRequired(ActionPriority.Idle);

			foreach (var living in m_livingList)
			{
				if (living.HasAction)
					living.PerformAction();
				else if (!forceMove)
					throw new Exception();
			}

			m_state = WorldState.TickDone;

			VDbg("SimultaneousWork Done");
		}



		bool SequentialWorkAvailable()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			if (RemoveLivingListContains(m_livingEnumerator.Current))
			{
				VDbg("WorkAvailable: Current living is to be removed");
				return true;
			}

			if (m_livingEnumerator.Current.HasAction)
			{
				VDbg("WorkAvailable: Living.HasAction");
				return true;
			}

			if (this.UseMaxMoveTime && DateTime.Now >= m_nextMove)
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
				var living = m_livingEnumerator.Current;

				if (RemoveLivingListContains(living))
					forceMove = true;

				if (!forceMove && !living.HasAction)
					break;

				living.PerformAction();

				var last = GetNextLivingSeq();
				if (last)
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

			MyDebug.WriteLine("-- Tick {0} started --", this.TickNumber);
			m_tickRequested = false;

			if (m_config.TickMethod == WorldTickMethod.Simultaneous)
			{
				foreach (var l in m_livingList)
					l.AI.ActionRequired(ActionPriority.High);

				var livings = m_livingList.
					Where(l => l.Controller != null).
					Where(l => !l.HasAction || (l.CurrentAction.Priority < ActionPriority.High && l.CurrentAction.UserID == 0));

				foreach (var l in livings)
					l.Controller.Send(new Messages.ActionRequiredMessage() { ObjectID = l.ObjectID });
			}

			m_state = WorldState.TickOngoing;

			if (TickEvent != null)
				TickEvent();

			if (m_config.TickMethod == WorldTickMethod.Simultaneous)
			{
				if (this.UseMaxMoveTime)
				{
					m_nextMove = DateTime.Now + m_config.MaxMoveTime;
					m_tickTimer.Change(m_config.MaxMoveTime, TimeSpan.FromTicks(-1));
				}
			}
			else if (m_config.TickMethod == WorldTickMethod.Sequential)
			{
				m_livingEnumerator = m_livingList.GetEnumerator();

				bool last = GetNextLivingSeq();
				if (last)
					throw new Exception("no livings");
			}
		}

		bool GetNextLivingSeq()
		{
			bool last = !m_livingEnumerator.MoveNext();

			if (last)
				return true;

			if (this.UseMaxMoveTime)
			{
				m_nextMove = DateTime.Now + m_config.MaxMoveTime;
				m_tickTimer.Change(m_config.MaxMoveTime, TimeSpan.FromTicks(-1));
			}

			var living = m_livingEnumerator.Current;
			if (!living.HasAction && !IsMoveForced())
				living.Controller.Send(new Messages.ActionRequiredMessage() { ObjectID = living.ObjectID });

			return false;
		}

		void EndTick()
		{
			VerifyAccess();

			if (this.UseMinTickTime)
			{
				m_nextTick = DateTime.Now + m_config.MinTickTime;
				m_tickTimer.Change(m_config.MinTickTime, TimeSpan.FromTicks(-1));
			}

			MyDebug.WriteLine("-- Tick {0} ended --", this.TickNumber);
			m_tickRequested = false;
			m_state = WorldState.TickEnded;
		}
	}
}
