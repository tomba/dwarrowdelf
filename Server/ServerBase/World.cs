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
		void InitializeWorld(World world);
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

	public partial class World : IWorld
	{
		class WorldConfig
		{
			public WorldTickMethod TickMethod { get; set; }

			// Require an user to be in game for ticks to proceed
			public bool RequireUser { get; set; }

			// Require user to request to proceed, before proceeding
			public bool RequireTickRequest { get; set; }

			// Maximum time for one living to make its move. After this time has passed, the living
			// will be skipped
			public TimeSpan MaxMoveTime { get; set; }

			// Minimum time between ticks. Ticks will never proceed faster than this.
			public TimeSpan MinTickTime { get; set; }
		}

		WorldConfig m_config = new WorldConfig
		{
			TickMethod = WorldTickMethod.Simultaneous,
			RequireUser = true,
			RequireTickRequest = true,
			MaxMoveTime = TimeSpan.FromMilliseconds(1000),
			MinTickTime = TimeSpan.FromMilliseconds(50),
		};

		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }
		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }

		public IArea Area { get; private set; }

		// only for debugging
		public bool IsWritable { get; private set; }

		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		bool m_verbose = false;

		WorldState m_state = WorldState.Idle;

		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter;

		List<Living>.Enumerator m_livingEnumerator;

		public event Action<IEnumerable<Change>, IEnumerable<Event>> HandleEndOfTurn;

		List<Change> m_changeList = new List<Change>();
		List<Event> m_eventList = new List<Event>();

		// XXX slow & bad
		public IEnumerable<Environment> Environments
		{
			get
			{
				Environment[] envs;
				lock (m_objectMap)
					envs = m_objectMap.Values.Select(wr => wr.Target).OfType<Environment>().ToArray();
				return envs;
			}
		}

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		int m_tickNumber;

		// time when next move has to happen
		DateTime m_nextMove = DateTime.MaxValue;

		// time when next tick will happen
		DateTime m_nextTick = DateTime.MinValue;

		// Timer is used out-of-tick to start the tick after m_minTickTime
		// and inside-tick to timeout player movements after m_maxMoveTime
		Timer m_tickTimer;

		// If the user has requested to proceed
		bool m_tickRequested;

		Thread m_worldThread;
		volatile bool m_exit = false;

		public event Action TickEvent;

		WorldLogger m_worldLogger;

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

			m_worldLogger = new WorldLogger(this);
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

			this.Area.InitializeWorld(this);

			var envs = this.Environments;
			foreach (var env in envs)
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

			m_worldLogger.Start();
			m_worldLogger.LogFullState();

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

			m_worldLogger.Stop();

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

			if (this.HasInstantInvokeWork)
			{
				VDbg("WorkAvailable: InstantInvoke");
				return true;
			}

			if (m_state == WorldState.Idle)
			{
				if (this.HasPreTickInvokeWork)
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

			foreach (Living l in m_livingList)
				l.AI.ActionRequired(ActionPriority.Idle);

			while (true)
			{
				Living living = m_livingEnumerator.Current;

				if (living == null)
					break;

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

		bool IsMoveForced()
		{
			return (this.UseMaxMoveTime && DateTime.Now >= m_nextMove) || m_tickRequested;
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

			// XXX making decision here is ok for Simultaneous mode, but not quite
			// for sequential...
			foreach (Living l in m_livingList)
				l.AI.ActionRequired(ActionPriority.High);

			if (m_config.TickMethod == WorldTickMethod.Simultaneous)
			{
				// This presumes that non-user controlled livings already have actions
				var events = m_livingList.
					Where(l => !l.HasAction).
					Select(l => new ActionRequiredEvent() { ObjectID = l.ObjectID });

				foreach (var e in events)
					AddEvent(e);
			}

			m_state = WorldState.TickOngoing;

			if (TickEvent != null)
				TickEvent();

			m_livingEnumerator = m_livingList.GetEnumerator();

			if (m_config.TickMethod == WorldTickMethod.Simultaneous)
			{
				m_livingEnumerator.MoveNext();

				if (this.UseMaxMoveTime)
				{
					m_nextMove = DateTime.Now + m_config.MaxMoveTime;
					m_tickTimer.Change(m_config.MaxMoveTime, TimeSpan.FromTicks(-1));
				}
			}
			else if (m_config.TickMethod == WorldTickMethod.Sequential)
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

			if (this.UseMaxMoveTime)
			{
				m_nextMove = DateTime.Now + m_config.MaxMoveTime;
				m_tickTimer.Change(m_config.MaxMoveTime, TimeSpan.FromTicks(-1));
			}

			if (m_config.TickMethod == WorldTickMethod.Sequential)
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

			if (this.UseMinTickTime)
			{
				m_nextTick = DateTime.Now + m_config.MinTickTime;
				m_tickTimer.Change(m_config.MinTickTime, TimeSpan.FromTicks(-1));
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

			IIdentifiable ob = null;

			lock (m_objectMap)
			{
				WeakReference weakref;

				if (m_objectMap.TryGetValue(objectID, out weakref))
				{
					ob = weakref.Target as IIdentifiable;

					if (ob == null)
						m_objectMap.Remove(objectID);
				}
			}

			return ob;
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
		public ItemObject[] IPItems
		{
			get { return m_objectMap.Values.Select(wr => wr.Target).OfType<ItemObject>().ToArray(); }
		}

		public Living[] IPLivings
		{
			get { return m_livingList.ToArray(); }
		}

		public IIdentifiable IPGet(object target)
		{
			IIdentifiable ob = null;

			if (target is int)
			{
				ob = FindObject(new ObjectID((int)target));
			}

			if (ob == null)
				throw new Exception(String.Format("object {0} not found", target));

			return ob;
		}
	}
}
