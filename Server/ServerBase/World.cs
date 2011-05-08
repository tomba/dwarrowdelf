using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
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

	[GameObject(UseRef = true)]
	public partial class World : IWorld
	{
		[Serializable]
		class WorldConfig
		{
			public WorldTickMethod TickMethod;

			// Require an user to be in game for ticks to proceed
			public bool RequireUser;

			// Require an controllables to be in game for ticks to proceed
			public bool RequireControllables;

			// Maximum time for one living to make its move. After this time has passed, the living
			// will be skipped
			public TimeSpan MaxMoveTime;

			// Minimum time between ticks. Ticks will never proceed faster than this.
			public TimeSpan MinTickTime;

			public bool SingleStep;
		}

		[GameProperty]
		WorldConfig m_config = new WorldConfig
		{
			TickMethod = WorldTickMethod.Simultaneous,
			RequireUser = true,
			RequireControllables = false,
			MaxMoveTime = TimeSpan.Zero,
			MinTickTime = TimeSpan.FromMilliseconds(50),
			SingleStep = false,
		};

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "World");

		// only for debugging
		public bool IsWritable { get; private set; }

		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		[GameProperty]
		Dictionary<ObjectID, IBaseGameObject> m_objectMap = new Dictionary<ObjectID, IBaseGameObject>();
		[GameProperty]
		int[] m_objectIDcounterArray;

		public event Action WorkEnded;
		public event Action<Change> WorldChanged;
		public event Action TickEnded;

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		Thread m_worldThread;
		volatile bool m_exit = false;

		WorldLogger m_worldLogger;

		InvokeList m_preTickInvokeList;
		InvokeList m_instantInvokeList;

		public World()
		{
			var maxType = Enum.GetValues(typeof(ObjectType)).Cast<int>().Max();
			m_objectIDcounterArray = new int[maxType + 1];

			m_worldLogger = new WorldLogger(this);

			m_preTickInvokeList = new InvokeList(this);
			m_instantInvokeList = new InvokeList(this);

			InitializeWorldTick();
		}

		public void Initialize(IArea area)
		{
			EnterWriteLock();

			trace.TraceInformation("Initializing area");
			var sw = Stopwatch.StartNew();
			area.InitializeWorld(this);
			sw.Stop();
			trace.TraceInformation("Initializing area took {0}", sw.Elapsed);

			ExitWriteLock();
		}

		public void Start()
		{
			Debug.Assert(m_worldThread == null);

			m_worldThread = new Thread(Main);
			m_worldThread.Name = "World";

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

		public void EnableSingleStep()
		{
			m_step = false;
			Thread.MemoryBarrier();
			m_config.SingleStep = true;
		}

		public void DisableSingleStep()
		{
			m_config.SingleStep = false;
			SignalWorld();
		}

		public void SingleStep()
		{
			m_step = true;
			SignalWorld();
		}

		public void SetMinTickTime(TimeSpan minTickTime)
		{
			m_config.MinTickTime = minTickTime;
		}

		void Main(object arg)
		{
			VerifyAccess();

			trace.TraceInformation("WorldMain");

			m_worldLogger.Start();
			m_worldLogger.LogFullState();

			EventWaitHandle initEvent = (EventWaitHandle)arg;
			initEvent.Set();

			while (m_exit == false)
			{
				m_worldSignal.WaitOne();
				Work();
			}

			m_worldLogger.Stop();

			trace.TraceInformation("WorldMain end");
		}

		void VerifyAccess()
		{
			if (m_worldThread != null && Thread.CurrentThread != m_worldThread)
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
			trace.TraceVerbose("SignalWorld");
			m_worldSignal.Set();
		}


		public void AddChange(Change change)
		{
			VerifyAccess();
			if (WorldChanged != null)
				WorldChanged(change);
		}

		internal void AddGameObject(IBaseGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				m_objectMap.Add(ob.ObjectID, ob);
		}

		internal void RemoveGameObject(IBaseGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				if (m_objectMap.Remove(ob.ObjectID) == false)
					throw new Exception();
		}

		public IBaseGameObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");


			lock (m_objectMap)
			{
				IBaseGameObject ob = null;

				if (m_objectMap.TryGetValue(objectID, out ob))
					return ob;
				else
					return null;
			}
		}

		public T FindObject<T>(ObjectID objectID) where T : class, IBaseGameObject
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		internal ObjectID GetNewObjectID(ObjectType objectType)
		{
			// XXX overflows
			//return new ObjectID(objectType, Interlocked.Increment(ref m_objectIDcounterArray[(int)objectType]));
			// XXX use a common counter to make debugging simpler
			return new ObjectID(objectType, Interlocked.Increment(ref m_objectIDcounterArray[0]));
		}

		// XXX slow & bad
		public IEnumerable<Environment> Environments
		{
			get
			{
				Environment[] envs;
				lock (m_objectMap)
					envs = m_objectMap.Values.OfType<Environment>().ToArray();
				return envs;
			}
		}
	}
}
