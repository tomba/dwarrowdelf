using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame.Server
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
		TickOngoing,
		TickEnded,
	}

	public class World
	{
		public IArea Area { get; private set; }

		// the same single world for everybody, for now
		public static World TheWorld;

		// only for debugging
		public bool IsWritable { get; private set; }
	
		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		bool m_verbose = false;

		WorldState m_state = WorldState.Idle;

		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter;

		List<Living> m_livingList = new List<Living>();
		List<Living>.Enumerator m_livingEnumerator;
		List<Living> m_addLivingList = new List<Living>();
		List<Living> m_removeLivingList = new List<Living>();

		public event Action<IEnumerable<Change>, IEnumerable<Event>> HandleEndOfTurn;

		List<Change> m_changeList = new List<Change>();
		List<Event> m_eventList = new List<Event>();

		List<ServerConnection> m_userList = new List<ServerConnection>();

		List<Environment> m_environments = new List<Environment>();
		public IEnumerable<Environment> Environments { get { return m_environments; } }

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		int m_tickNumber;

		WorldTickMethod m_tickMethod = WorldTickMethod.Simultaneous;

		// Maximum time for one living to make its move. After this time has passed, the living
		// will be skipped
		bool m_useMaxMoveTime = false;
		TimeSpan m_maxMoveTime = TimeSpan.FromMilliseconds(1000);
		DateTime m_nextMove = DateTime.MaxValue;

		// Minimum time between ticks. Ticks will never proceed faster than this.
		bool m_useMinTickTime = true;
		TimeSpan m_minTickTime = TimeSpan.FromMilliseconds(50);
		DateTime m_nextTick = DateTime.MinValue;

		// Timer is used out-of-tick to start the tick after m_minTickTime
		// and inside-tick to timeout player movements after m_maxMoveTime
		Timer m_tickTimer;

		// If the user has requested to proceed
		bool m_tickRequested;

		// Require an user to be in game for ticks to proceed
		bool m_requireUser = true;

		// Require user to request to proceed, before proceeding
		bool m_requireTickRequest = true;

		class InvokeInfo
		{
			public Delegate Action;
			public object[] Args;
		}

		List<InvokeInfo> m_preTickInvokeList = new List<InvokeInfo>();
		List<InvokeInfo> m_instantInvokeList = new List<InvokeInfo>();

		Thread m_worldThread;
		volatile bool m_exit = false;

		[Conditional("DEBUG")]
		void VDbg(string format, params object[] args)
		{
			if (m_verbose)
				MyDebug.WriteLine(format, args);
		}

		public World(IArea area)
		{
			this.Area = area;
			m_tickTimer = new Timer(this.TickTimerCallback);

			m_worldThread = new Thread(Main);
			m_worldThread.Name = "World";
		}

		public void Start()
		{
			Debug.Assert(!m_worldThread.IsAlive);

			using (var initEvent = new ManualResetEvent(false))
			{
				m_worldThread.Start(initEvent);
				initEvent.WaitOne();
			}
		}

		public void Stop()
		{
			Debug.Assert(m_worldThread.IsAlive);

			m_exit = true;
			SignalWorld();
			m_worldThread.Join();
		}

		void Init()
		{
			EnterWriteLock();

			this.Area.InitializeWorld(this, m_environments);

			foreach (var env in m_environments)
				env.MapChanged += this.MapChangedCallback;

			ExitWriteLock();

			// process any changes from world initialization
			if (HandleEndOfTurn != null)
				HandleEndOfTurn(m_changeList, m_eventList);
			m_changeList.Clear();
			m_eventList.Clear();
		}

		void Main(object arg)
		{
			VerifyAccess();

			MyDebug.WriteLine("WorldMain");

			Init();

			EventWaitHandle initEvent = (EventWaitHandle)arg;
			initEvent.Set();

			while (m_exit == false)
			{
				m_worldSignal.WaitOne();

				do
				{
					Work();
				} while (WorkAvailable());
			}

			MyDebug.WriteLine("WorldMain end");
		}

		void VerifyAccess()
		{
			if (Thread.CurrentThread != m_worldThread)
				throw new Exception();
		}

		void EnterWriteLock()
		{
			m_rwLock.EnterWriteLock();
#if DEBUG
			this.IsWritable = true;
#endif
		}

		void ExitWriteLock()
		{
#if DEBUG
			this.IsWritable = false;
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

		public int TickNumber
		{
			get { return m_tickNumber; }
		}

		// thread safe
		internal void AddUser(ServerConnection user)
		{
			lock (m_userList)
				m_userList.Add(user);

			SignalWorld();
		}

		// thread safe
		internal void RemoveUser(ServerConnection user)
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
			VerifyAccess();

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
			VerifyAccess();

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
		public void BeginInvoke(Delegate callback, params object[] args)
		{
			lock (m_preTickInvokeList)
				m_preTickInvokeList.Add(new InvokeInfo() { Action = callback, Args = args});

			SignalWorld();
		}

		void ProcessInvokeList()
		{
			VerifyAccess();

			lock (m_preTickInvokeList)
			{
				if (m_preTickInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} invoke callbacks", m_preTickInvokeList.Count);
				foreach (InvokeInfo a in m_preTickInvokeList)
					a.Action.DynamicInvoke(a.Args); // XXX dynamicinvoke
				m_preTickInvokeList.Clear();
			}
		}


		// thread safe
		public void BeginInvokeInstant(Action<object> callback)
		{
			BeginInvokeInstant(callback, null);
		}

		// thread safe
		public void BeginInvokeInstant(Delegate callback, params object[] args)
		{
			lock (m_instantInvokeList)
				m_instantInvokeList.Add(new InvokeInfo() { Action = callback, Args = args });

			SignalWorld();
		}

		void ProcessInstantInvokeList()
		{
			VerifyAccess();

			lock (m_instantInvokeList)
			{
				if (m_instantInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} instant invoke callbacks", m_instantInvokeList.Count);
				foreach (InvokeInfo a in m_instantInvokeList)
					a.Action.DynamicInvoke(a.Args); // XXX dynamicinvoke
				m_instantInvokeList.Clear();
			}
		}



		// thread safe
		public void SignalWorld()
		{
			VDbg("SignalWorld");
			m_worldSignal.Set();
		}

		internal void RequestTick()
		{
			m_tickRequested = true;
			SignalWorld();
		}

		void TickTimerCallback(object stateInfo)
		{
			VDbg("TickTimerCallback");
			SignalWorld();
		}

		bool IsTimeToStartTick()
		{
			VerifyAccess();

			if (m_state != WorldState.Idle)
				return false;

			if (m_useMinTickTime && DateTime.Now < m_nextTick)
				return false;

			if (m_requireUser && m_tickRequested == false)
			{
				lock (m_userList)
					if (m_userList.Count == 0)
						return false;
			}

			if (m_requireTickRequest && m_tickRequested == false)
				return false;

			return true;
		}

		void Work()
		{
			VerifyAccess();

			EnterWriteLock();

			ProcessInstantInvokeList();

			if (m_state == WorldState.Idle)
			{
				//MyDebug.WriteLine("-- Pretick {0} events --", m_tickNumber + 1);

				ProcessInvokeList();
				ProcessAddLivingList();
				ProcessRemoveLivingList();

				//MyDebug.WriteLine("-- Pretick {0} events done --", m_tickNumber + 1);

				if (IsTimeToStartTick())
				{
					// XXX making decision here is ok for Simultaneous mode, but not quite
					// for sequential...
					// note: write lock is off, actors can take read-lock and process in the
					// background
					// perhaps here we should also send ActionRequestEvents?
					foreach (Living l in m_livingList)
						l.Actor.DetermineAction();

					StartTick();
				}
			}

			if (m_state == WorldState.TickOngoing)
			{
				if (m_tickMethod == WorldTickMethod.Simultaneous)
					SimultaneousWork();
				else if (m_tickMethod == WorldTickMethod.Sequential)
					SequentialWork();
				else
					throw new NotImplementedException();
			}

			ExitWriteLock();

			// no point in entering read lock here, as this thread is the only one that can get a write lock
			if (HandleEndOfTurn != null)
				HandleEndOfTurn(m_changeList, m_eventList);
			m_changeList.Clear();
			m_eventList.Clear();

			if (m_state == WorldState.TickEnded)
			{
				// perhaps this is not needed for anything
				m_state = WorldState.Idle;
			}
		}

		bool WorkAvailable()
		{
			VerifyAccess();

			lock (m_instantInvokeList)
				if (m_instantInvokeList.Count > 0)
				{
					VDbg("WorkAvailable: InstantInvoke");
					return true;
				}

			if (m_state == WorldState.Idle)
			{
				lock (m_preTickInvokeList)
					if (m_preTickInvokeList.Count > 0)
					{
						VDbg("WorkAvailable: PreTickInvoke");
						return true;
					}

				lock (m_addLivingList)
					if (m_addLivingList.Count > 0)
					{
						VDbg("WorkAvailable: AddLiving");
						return true;
					}

				lock (m_removeLivingList)
					if (m_removeLivingList.Count > 0)
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
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			if (m_livingList.All(l => l.HasAction))
				return true;

			if (m_useMaxMoveTime && DateTime.Now >= m_nextMove)
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

			EndTick();

			VDbg("SimultaneousWork Done");
		}



		bool SequentialWorkAvailable()
		{
			VerifyAccess();
			Debug.Assert(m_state == WorldState.TickOngoing);

			lock (m_removeLivingList)
			{
				if (m_removeLivingList.Contains(m_livingEnumerator.Current))
				{
					VDbg("WorkAvailable: Current living is to be removed");
					return true;
				}
			}

			if (m_livingEnumerator.Current.HasAction)
			{
				VDbg("WorkAvailable: Living.HasAction");
				return true;
			}

			if (m_useMaxMoveTime && DateTime.Now >= m_nextMove)
			{
				VDbg("WorkAvailable: NextMoveTime");
				return true;
			}

			return false;
		}

		bool IsMoveForced()
		{
			return (m_useMaxMoveTime && DateTime.Now >= m_nextMove) || m_tickRequested;
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

				lock (m_removeLivingList)
					if (m_removeLivingList.Contains(living))
						forceMove = true;

				if (!forceMove && !living.HasAction)
					break;

				living.PerformAction();

				var last = GetNextLivingSeq();
				if (last)
				{
					VDbg("last living handled");
					EndTick();
					break;
				}
			}

			VDbg("SequentialWork Done");
		}

		void StartTick()
		{
			VerifyAccess();

			m_tickNumber++;
			AddEvent(new TickChangeEvent(m_tickNumber));

			MyDebug.WriteLine("-- Tick {0} started --", m_tickNumber);
			m_tickRequested = false;

			if (m_tickMethod == WorldTickMethod.Simultaneous)
			{
				// This presumes that non-user controlled livings already have actions
				var events = m_livingList.
					Where(l => !l.HasAction).
					Select(l => new ActionRequiredEvent() { ObjectID = l.ObjectID });

				foreach (var e in events)
					AddEvent(e);
			}

			m_livingEnumerator = m_livingList.GetEnumerator();

			m_state = WorldState.TickOngoing;

			if (m_tickMethod == WorldTickMethod.Simultaneous)
			{
				if (!m_livingEnumerator.MoveNext())
					throw new Exception("no livings");

				if (m_useMaxMoveTime)
				{
					m_nextMove = DateTime.Now + m_maxMoveTime;
					m_tickTimer.Change(m_maxMoveTime, TimeSpan.FromTicks(-1));
				}
			}
			else if (m_tickMethod == WorldTickMethod.Sequential)
			{
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

			if (m_useMaxMoveTime)
			{
				m_nextMove = DateTime.Now + m_maxMoveTime;
				m_tickTimer.Change(m_maxMoveTime, TimeSpan.FromTicks(-1));
			}

			if (m_tickMethod == WorldTickMethod.Sequential)
			{
				var living = m_livingEnumerator.Current;
				if (!living.HasAction && !IsMoveForced())
					this.AddEvent(new ActionRequiredEvent() { ObjectID = living.ObjectID });
			}

			return false;
		}

		void EndTick()
		{
			VerifyAccess();

			if (m_useMinTickTime)
			{
				m_nextTick = DateTime.Now + m_minTickTime;
				m_tickTimer.Change(m_minTickTime, TimeSpan.FromTicks(-1));
			}

			MyDebug.WriteLine("-- Tick {0} ended --", m_tickNumber);
			m_tickRequested = false;
			m_state = WorldState.TickEnded;
		}

		public void AddChange(Change change)
		{
			VerifyAccess();
			m_changeList.Add(change);
		}

		public void AddEvent(Event @event)
		{
			VerifyAccess();
			m_eventList.Add(@event);
		}

		void MapChangedCallback(Environment map, IntPoint3D l, TileData tileData)
		{
			VerifyAccess();
			AddChange(new MapChange(map, l, tileData));
		}

		internal void AddGameObject(IIdentifiable ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				m_objectMap.Add(ob.ObjectID, new WeakReference(ob));
		}

		internal void RemoveGameObject(IIdentifiable ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				if (m_objectMap.Remove(ob.ObjectID) == false)
					throw new Exception();
		}

		public IIdentifiable FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
			{
				if (m_objectMap.ContainsKey(objectID))
				{
					WeakReference weakref = m_objectMap[objectID];
					if (weakref.IsAlive)
						return (IIdentifiable)m_objectMap[objectID].Target;
					else
						m_objectMap.Remove(objectID);
				}
			}

			return null;
		}

		public T FindObject<T>(ObjectID objectID) where T : class, IIdentifiable
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		internal ObjectID GetNewObjectID()
		{
			return new ObjectID(Interlocked.Increment(ref m_objectIDcounter));
		}


		/* helpers for ironpython */
		public ItemObject[] Items
		{
			get { return m_objectMap.Values.Select(wr => wr.Target).OfType<ItemObject>().ToArray(); }
		}

		public Living[] Livings
		{
			get { return m_livingList.ToArray(); }
		}
	}
}
