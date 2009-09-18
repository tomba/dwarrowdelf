using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame
{
	// XXX move somewhere else, but inside Server side */
	public interface IArea
	{
		void InitializeWorld(World world, IList<Environment> environments);
	}

	enum WorldTickMethod
	{
		Simultaneous,
		Sequential,
	}

	enum WorldState
	{
		Idle,
		TurnOngoing,
		TurnEnded,
	}

	public class World
	{
		public IArea Area { get; private set; }
		public IAreaData AreaData { get; private set; }

		// the same single world for everybody, for now
		public static World TheWorld;

		// only for debugging
		public bool IsWriteable { get; private set; }
	
		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		bool m_verbose = false;

		WorldState m_state = WorldState.Idle;

		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter = 0;

		List<Living> m_livingList = new List<Living>();
		List<Living>.Enumerator m_livingEnumerator;
		List<Living> m_addLivingList = new List<Living>();
		List<Living> m_removeLivingList = new List<Living>();

		public event Action<IEnumerable<Change>> HandleChangesEvent;
		public event Action<IEnumerable<Event>> HandleEventsEvent;

		List<Change> m_changeList = new List<Change>();
		List<Event> m_eventList = new List<Event>();

		List<ServerService> m_userList = new List<ServerService>();

		List<Environment> m_environments = new List<Environment>();
		public IEnumerable<Environment> Environments { get { return m_environments; } }

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		int m_turnNumber = 0;

		WorldTickMethod m_tickMethod = WorldTickMethod.Sequential;

		// maximum time for one living to make its move
		bool m_useMaxMoveTime = false;
		TimeSpan m_maxMoveTime = TimeSpan.FromMilliseconds(1000);
		DateTime m_nextMove = DateTime.MaxValue;

		// minimum time between turns
		bool m_useMinTurnTime = false;
		TimeSpan m_minTurnTime = TimeSpan.FromMilliseconds(1000);
		DateTime m_nextTurn = DateTime.MinValue;

		// Timer is used out-of-turn to start the turn after m_minTurnTime
		// and inside-turn to timeout player movements after m_maxMoveTime
		Timer m_tickTimer;

		// If the user has requested to proceed
		bool m_turnRequested;

		// Require an user to be in game for turns to proceed
		bool m_requireUser = true;

		class InvokeInfo
		{
			public Action<object> Action;
			public object Data;
		}

		List<InvokeInfo> m_preTurnInvokeList = new List<InvokeInfo>();
		List<InvokeInfo> m_instantInvokeList = new List<InvokeInfo>();

		bool m_workActive;
		object m_workLock = new object();


		public World(IArea area, IAreaData areaData)
		{
			this.Area = area;
			this.AreaData = areaData;
			m_tickTimer = new Timer(this.TickTimerCallback);

			// mark as active for the initialization
			m_workActive = true;
			EnterWriteLock();

			area.InitializeWorld(this, m_environments);

			foreach (var env in m_environments)
				env.MapChanged += this.MapChangedCallback;

			// process any changes from world initialization
			ProcessChanges();
			ProcessEvents();

			m_workActive = false;
			ExitWriteLock();

			ThreadPool.RegisterWaitForSingleObject(m_worldSignal, WorldSignalledWork, null, -1, false);
		}

		void EnterWriteLock()
		{
			m_rwLock.EnterWriteLock();
#if DEBUG
			this.IsWriteable = true;
#endif
		}

		void ExitWriteLock()
		{
#if DEBUG
			this.IsWriteable = false;
#endif
			m_rwLock.ExitWriteLock();
		}

		public void EnterReadLock()
		{
			m_rwLock.EnterReadLock();
		}

		public void ExitReadLock()
		{
			m_rwLock.ExitReadLock();
		}

		public int TurnNumber
		{
			get { return m_turnNumber; }
		}

		// thread safe
		internal void AddUser(ServerService user)
		{
			lock (m_userList)
				m_userList.Add(user);

			SignalWorld();
		}

		// thread safe
		internal void RemoveUser(ServerService user)
		{
			lock (m_userList)
				m_userList.Remove(user);

			SignalWorld();
		}

		// thread safe
		internal void AddLiving(Living living)
		{
			lock (m_addLivingList)
				m_addLivingList.Add(living);

			SignalWorld();
		}

		void ProcessAddLivingList()
		{
			Debug.Assert(m_workActive);

			lock (m_addLivingList)
			{
				if (m_addLivingList.Count > 0)
					MyDebug.WriteLine("Processing {0} add livings", m_addLivingList.Count);
				foreach (var living in m_addLivingList)
				{
					Debug.Assert(!m_livingList.Contains(living));
					m_livingList.Add(living);
				}

				m_addLivingList.Clear();
			}
		}

		// thread safe
		internal void RemoveLiving(Living living)
		{
			lock (m_removeLivingList)
				m_removeLivingList.Add(living);

			SignalWorld();
		}

		void ProcessRemoveLivingList()
		{
			Debug.Assert(m_workActive);

			lock (m_removeLivingList)
			{
				if (m_removeLivingList.Count > 0)
					MyDebug.WriteLine("Processing {0} remove livings", m_removeLivingList.Count);
				foreach (var living in m_removeLivingList)
				{
					bool removed = m_livingList.Remove(living);
					Debug.Assert(removed);
				}

				m_removeLivingList.Clear();
			}
		}

		// thread safe
		public void BeginInvoke(Action<object> callback)
		{
			BeginInvoke(callback, null);
		}

		// thread safe
		public void BeginInvoke(Action<object> callback, object data)
		{
			lock (m_preTurnInvokeList)
				m_preTurnInvokeList.Add(new InvokeInfo() { Action = callback, Data = data });

			SignalWorld();
		}

		void ProcessInvokeList()
		{
			Debug.Assert(m_workActive);

			lock (m_preTurnInvokeList)
			{
				if (m_preTurnInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} invoke callbacks", m_preTurnInvokeList.Count);
				foreach (InvokeInfo a in m_preTurnInvokeList)
					a.Action(a.Data);
				m_preTurnInvokeList.Clear();
			}
		}



		// thread safe
		public void BeginInvokeInstant(Action<object> callback)
		{
			BeginInvokeInstant(callback, null);
		}

		// thread safe
		public void BeginInvokeInstant(Action<object> callback, object data)
		{
			lock (m_instantInvokeList)
				m_instantInvokeList.Add(new InvokeInfo() { Action = callback, Data = data });

			SignalWorld();
		}

		void ProcessInstantInvokeList()
		{
			Debug.Assert(m_workActive);

			lock (m_instantInvokeList)
			{
				if (m_instantInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} instant invoke callbacks", m_instantInvokeList.Count);
				foreach (InvokeInfo a in m_instantInvokeList)
					a.Action(a.Data);
				m_instantInvokeList.Clear();
			}
		}



		// thread safe
		public void SignalWorld()
		{
			if (m_verbose)
				MyDebug.WriteLine("SignalWorld");
			m_worldSignal.Set();
		}

		internal void RequestTurn()
		{
			m_turnRequested = true;
			SignalWorld();
		}

		void TickTimerCallback(object stateInfo)
		{
			if (m_verbose)
				MyDebug.WriteLine("TickTimerCallback");
			SignalWorld();
		}

		// Called whenever world is signalled
		void WorldSignalledWork(object state, bool timedOut)
		{
			lock (m_workLock)
			{
				if (m_workActive)
					return;
				m_workActive = true;
			}

			if (m_verbose)
				MyDebug.WriteLine("WorldSignalledWork");

			while (true)
			{
				Work();

				lock (m_workLock)
				{
					if (!WorkAvailable())
					{
						m_workActive = false;
						break;
					}
				}
			}

			if (m_verbose)
				MyDebug.WriteLine("WorldSignalledWork done");
		}

		bool IsTimeToStartTurn()
		{
			Debug.Assert(m_workActive);

			if (m_state != WorldState.Idle)
				return false;

			if (m_useMinTurnTime && DateTime.Now < m_nextTurn)
				return false;

			if (m_requireUser && m_turnRequested == false)
			{
				lock (m_userList)
					if (m_userList.Count == 0)
						return false;
			}

			return true;
		}

		void Work()
		{
			EnterWriteLock();
			ProcessInstantInvokeList();
			ExitWriteLock();

			if (m_state == WorldState.Idle)
			{
				MyDebug.WriteLine("-- Preturn {0} events --", m_turnNumber + 1);

				EnterWriteLock();
				ProcessInvokeList();
				ProcessAddLivingList();
				ProcessRemoveLivingList();
				ExitWriteLock();

				MyDebug.WriteLine("-- Preturn {0} events done --", m_turnNumber + 1);

				if (IsTimeToStartTurn())
				{
					// XXX making decision here is ok for Simultaneous mode, but not quite
					// for sequential...
					// note: write lock is off, actors can take read-lock and process in the
					// background
					foreach (Living l in m_livingList)
						l.Actor.DetermineAction();

					StartTurn();
				}
			}

			if (m_state == WorldState.TurnOngoing)
			{
				EnterWriteLock();
				if (m_tickMethod == WorldTickMethod.Simultaneous)
					SimultaneousWork();
				else if (m_tickMethod == WorldTickMethod.Sequential)
					SequentialWork();
				else
					throw new NotImplementedException();
				ExitWriteLock();
			}

			ProcessChanges();
			ProcessEvents();

			if (m_state == WorldState.TurnEnded)
			{
				// perhaps this is not needed for anything
				m_state = WorldState.Idle;
			}
		}

		bool WorkAvailable()
		{
			Debug.Assert(m_workActive);

			lock (m_instantInvokeList)
				if (m_instantInvokeList.Count > 0)
					return true;

			if (m_state == WorldState.Idle)
			{
				lock (m_preTurnInvokeList)
					if (m_preTurnInvokeList.Count > 0)
						return true;

				lock (m_addLivingList)
					if (m_addLivingList.Count > 0)
						return true;

				lock (m_removeLivingList)
					if (m_removeLivingList.Count > 0)
						return true;

				if (IsTimeToStartTurn())
					return true;

				return false;
			}
			else if (m_state == WorldState.TurnOngoing)
			{
				if (m_tickMethod == WorldTickMethod.Simultaneous)
					return SimultaneousWorkAvailable();
				else if (m_tickMethod == WorldTickMethod.Sequential)
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
			Debug.Assert(m_workActive);
			Debug.Assert(m_state == WorldState.TurnOngoing);

			if (m_livingList.All(l => l.HasAction))
				return true;

			if (m_useMaxMoveTime && DateTime.Now >= m_nextMove)
				return true;

			return false;
		}

		void SimultaneousWork()
		{
			Debug.Assert(m_workActive);
			Debug.Assert(m_state == WorldState.TurnOngoing);

			bool forceMove = m_useMaxMoveTime && DateTime.Now >= m_nextMove;

			if (m_verbose)
				MyDebug.WriteLine("SimultaneousWork");

			if (!forceMove && !m_livingList.All(l => l.HasAction))
				return;

			if (!forceMove)
				Debug.Assert(m_livingList.All(l => l.HasAction));

			while (true)
			{
				Living living = m_livingEnumerator.Current;

				if (living.HasAction)
					living.PerformAction();
				else if (!forceMove)
					throw new Exception();

				if (m_livingEnumerator.MoveNext() == false)
					break;
			}

			EndTurn();

			if (m_verbose)
				MyDebug.WriteLine("SimultaneousWork Done");
		}



		bool SequentialWorkAvailable()
		{
			Debug.Assert(m_state == WorldState.TurnOngoing);

			if (m_livingEnumerator.Current.HasAction)
				return true;

			if (m_useMaxMoveTime && DateTime.Now >= m_nextMove)
				return true;

			return false;
		}

		void SequentialWork()
		{
			Debug.Assert(m_workActive);
			Debug.Assert(m_state == WorldState.TurnOngoing);

			bool forceMove = m_useMaxMoveTime && DateTime.Now >= m_nextMove;

			if (m_verbose)
				MyDebug.WriteLine("SequentialWork");

			while (true)
			{
				var living = m_livingEnumerator.Current;

				if (!forceMove && !living.HasAction)
				{
					if (m_useMaxMoveTime)
					{
						m_nextMove = DateTime.Now + m_maxMoveTime;
						m_tickTimer.Change(m_maxMoveTime, TimeSpan.FromTicks(-1));
					}

					// XXX this probably needs to be sent elsewhere also
					this.AddEvent(new ActionRequiredEvent() { ObjectID = living.ObjectID });

					break;
				}

				if (living.HasAction)
					living.PerformAction();

				bool last = !m_livingEnumerator.MoveNext();
				if (last)
				{
					if (m_verbose)
						MyDebug.WriteLine("last living handled");
					EndTurn();
					break;
				}
			}


			if (m_verbose)
				MyDebug.WriteLine("SequentialWork Done");
		}

		void StartTurn()
		{
			Debug.Assert(m_workActive);

			m_turnNumber++;
			AddEvent(new TurnChangeEvent(m_turnNumber));

			MyDebug.WriteLine("-- Turn {0} started --", m_turnNumber);

			m_livingEnumerator = m_livingList.GetEnumerator();
			if (m_livingEnumerator.MoveNext() == false)
				throw new Exception("no livings");

			m_state = WorldState.TurnOngoing;

			if (m_useMaxMoveTime)
			{
				m_nextMove = DateTime.Now + m_maxMoveTime;
				m_tickTimer.Change(m_maxMoveTime, TimeSpan.FromTicks(-1));
			}
		}

		void EndTurn()
		{
			Debug.Assert(m_workActive);

			if (m_useMinTurnTime)
			{
				m_nextTurn = DateTime.Now + m_minTurnTime;
				m_tickTimer.Change(m_minTurnTime, TimeSpan.FromTicks(-1));
			}

			MyDebug.WriteLine("-- Turn {0} ended --", m_turnNumber);
			m_turnRequested = false;
			m_state = WorldState.TurnEnded;
		}

		public void AddChange(Change change)
		{
			Debug.Assert(m_workActive);
			m_changeList.Add(change);
		}

		void ProcessChanges()
		{
			Debug.Assert(m_workActive);

			if (HandleChangesEvent != null)
				HandleChangesEvent(m_changeList);

			m_changeList.Clear();
		}

		public void AddEvent(Event @event)
		{
			Debug.Assert(m_workActive);
			m_eventList.Add(@event);
		}

		void ProcessEvents()
		{
			Debug.Assert(m_workActive);

			if (HandleEventsEvent != null)
				HandleEventsEvent(m_eventList);

			m_eventList.Clear();
		}

		void MapChangedCallback(Environment map, IntPoint3D l, int terrainID)
		{
			Debug.Assert(m_workActive);
			AddChange(new MapChange(map, l, terrainID));
		}

		public ServerGameObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			lock (m_objectMap)
			{
				if (m_objectMap.ContainsKey(objectID))
				{
					WeakReference weakref = m_objectMap[objectID];
					if (weakref.IsAlive)
						return (ServerGameObject)m_objectMap[objectID].Target;
					else
						m_objectMap.Remove(objectID);
				}
			}

			return null;
		}

		internal void AddGameObject(ServerGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			lock (m_objectMap)
				m_objectMap.Add(ob.ObjectID, new WeakReference(ob));
		}

		internal ObjectID GetNewObjectID()
		{
			return new ObjectID(Interlocked.Increment(ref m_objectIDcounter));
		}
	}
}
