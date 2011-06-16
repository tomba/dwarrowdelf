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
		public event Action TickStarting;
		public event Action TickEnded;

		public event Action<Living> TurnStarting;
		public event Action<Living> TurnEnded;

		public event Action WorkEnded;

		// XXX hackish
		public event Action HandleMessagesEvent;

		[SaveGameProperty]
		public int TickNumber { get; private set; }

		enum WorldState
		{
			Idle,
			TickOngoing,
			TickDone,
			TickEnded,
		}

		WorldState m_state = WorldState.Idle;

		bool m_okToStartTick = false;
		bool m_proceedTurn = false;

		public void SetOkToStartTick()
		{
			trace.TraceVerbose("SetOkToStartTick");
			VerifyAccess();
			m_okToStartTick = true;
		}

		public void SetProceedTurn()
		{
			trace.TraceVerbose("SetProceedTurn");
			VerifyAccess();
			m_proceedTurn = true;
		}

		public bool Work()
		{
			// Hack
			if (m_worldThread == null)
				m_worldThread = Thread.CurrentThread;

			VerifyAccess();

			EnterWriteLock();

			m_instantInvokeList.ProcessInvokeList();

			if (HandleMessagesEvent != null)
				HandleMessagesEvent();

			bool again = true;

			if (m_state == WorldState.Idle)
			{
				m_preTickInvokeList.ProcessInvokeList();
				m_livings.Process();

				if (m_okToStartTick)
					StartTick();
				else
					again = false;
			}

			if (m_state == WorldState.TickOngoing)
			{
				if (this.TickMethod == WorldTickMethod.Simultaneous)
					again = SimultaneousWork();
				else if (this.TickMethod == WorldTickMethod.Sequential)
					again = SequentialWork();
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

			return again;
		}

		bool SimultaneousWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			trace.TraceVerbose("SimultaneousWork");

			if (!m_proceedTurn)
				return false;

			m_proceedTurn = false;

			foreach (var living in m_livings.List)
				living.TurnPreRun();

			foreach (var living in m_livings.List.Where(l => l.HasAction))
				living.PerformAction();

			EndTurn();

			m_state = WorldState.TickDone;

			trace.TraceVerbose("SimultaneousWork Done");

			return true;
		}


		bool SequentialWork()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			Debug.Assert(false); // broken

			if (m_livings.List.Count == 0)
			{
				trace.TraceVerbose("no livings to handled");
				m_state = WorldState.TickDone;
				return true;
			}

			bool forceMove = m_proceedTurn;

			bool again = true;

			while (true)
			{
				var living = m_livingEnumerator.Current;

				if (m_livings.RemoveList.Contains(living))
					forceMove = true;

				if (!forceMove && !living.HasAction)
				{
					again = false;
					break;
				}

				m_proceedTurn = false;

				living.TurnPreRun();

				living.PerformAction();

				EndTurn(living);

				bool ok = m_livingEnumerator.MoveNext();
				if (ok)
				{
					StartTurnSequential(m_livingEnumerator.Current);
				}
				else
				{
					trace.TraceVerbose("last living handled");
					m_state = WorldState.TickDone;
					break;
				}
			}

			trace.TraceVerbose("SequentialWork Done");

			return again;
		}

		void StartTick()
		{
			VerifyAccess();

			this.TickNumber++;
			AddChange(new TickStartChange(this.TickNumber));

			trace.TraceInformation("-- Tick {0} started --", this.TickNumber);

			m_state = WorldState.TickOngoing;

			if (TickStarting != null)
				TickStarting();

			if (this.TickMethod == WorldTickMethod.Simultaneous)
			{
				StartTurnSimultaneous();
			}
			else if (this.TickMethod == WorldTickMethod.Sequential)
			{
				m_livingEnumerator.Reset();

				bool ok = m_livingEnumerator.MoveNext();
				if (ok)
					StartTurnSequential(m_livingEnumerator.Current);
			}
		}

		void StartTurnSimultaneous()
		{
			foreach (var living in m_livings.List)
				living.TurnStarted();

			AddChange(new TurnStartChange());

			if (TurnStarting != null)
				TurnStarting(null);
		}

		void StartTurnSequential(Living living)
		{
			living.TurnStarted();

			AddChange(new TurnStartChange(living));

			if (TurnStarting != null)
				TurnStarting(living);
		}

		void EndTurn(Living living = null)
		{
			AddChange(new TurnEndChange(living));

			if (TurnEnded != null)
				TurnEnded(living);
		}

		void EndTick()
		{
			VerifyAccess();

			trace.TraceInformation("-- Tick {0} ended --", this.TickNumber);
			m_state = WorldState.TickEnded;

			m_okToStartTick = false;

			if (TickEnded != null)
				TickEnded();
		}
	}
}
