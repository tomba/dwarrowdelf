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

		public IArea Area { get; private set; }

		// only for debugging
		public bool IsWritable { get; private set; }

		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		bool m_verbose = false;

		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter;

		public event Action<IEnumerable<Change>, IEnumerable<Event>> HandleEndOfTurn;

		List<Change> m_changeList = new List<Change>();
		List<Event> m_eventList = new List<Event>();

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		Thread m_worldThread;
		volatile bool m_exit = false;

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
			m_worldThread = new Thread(Main);
			m_worldThread.Name = "World";

			m_worldLogger = new WorldLogger(this);

			InitializeWorldTick();
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

		// thread safe
		public void SignalWorld()
		{
			VDbg("SignalWorld");
			m_worldSignal.Set();
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
