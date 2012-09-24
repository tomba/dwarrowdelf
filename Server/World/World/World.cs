using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public enum WorldTickMethod
	{
		Simultaneous,
		Sequential,
	}

	[SaveGameObjectByRef]
	public sealed partial class World : IWorld
	{
		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "World");

		// XXX where should this be?
		public LivingVisionMode LivingVisionMode { get { return LivingVisionMode.LOS; } }

		// only for debugging
		public bool IsWritable { get; private set; }

		ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();

		[SaveGameProperty]
		Dictionary<ObjectID, BaseObject> m_objectMap;
		[SaveGameProperty]
		int[] m_objectIDcounterArray;

		public event Action<Change> WorldChanged;
		public event Action<GameReport> ReportReceived;

		InvokeList m_preTickInvokeList;
		InvokeList m_instantInvokeList;

		[SaveGameProperty]
		Random m_random;

		Thread m_worldThread;

		[SaveGameProperty]
		public WorldTickMethod TickMethod { get; private set; }

		[SaveGameProperty]
		public GameMode GameMode { get; private set; }

		World()
		{
			m_preTickInvokeList = new InvokeList(this);
			m_instantInvokeList = new InvokeList(this);

			if (this.TickMethod == WorldTickMethod.Sequential)
				m_livingEnumerator = new LivingEnumerator(m_livings.List);
		}

		World(SaveGameContext ctx)
			: this()
		{
		}

		public World(GameMode gameMode, WorldTickMethod tickMethod)
			: this()
		{
			this.GameMode = gameMode;
			this.TickMethod = tickMethod;

			m_objectMap = new Dictionary<ObjectID, BaseObject>();
			m_livings = new ProcessableList<LivingObject>();
			m_random = new Random();

			m_objectIDcounterArray = new int[EnumHelpers.GetEnumMax<ObjectType>() + 1];

			m_state = WorldState.Idle;
		}

		public void Initialize(Action initializer)
		{
			EnterWriteLock();

			trace.TraceInformation("Initializing area");
			var m_initSw = Stopwatch.StartNew();

			initializer();

			m_initSw.Stop();
			trace.TraceInformation("Initializing area took {0} ms", m_initSw.ElapsedMilliseconds);

			ExitWriteLock();
		}

		public Random Random { get { return m_random; } }

		// Hack to do some verifying that all calls come from the same thread (world is not multithread safe)
		void VerifyAccess()
		{
			if (m_worldThread != null && m_worldThread != Thread.CurrentThread)
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

		public void AddChange(Change change)
		{
			VerifyAccess();
			if (WorldChanged != null)
				WorldChanged(change);
		}

		public void AddReport(GameReport report)
		{
			VerifyAccess();
			if (ReportReceived != null)
				ReportReceived(report);
		}

		internal void AddGameObject(BaseObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				m_objectMap.Add(ob.ObjectID, ob);
		}

		internal void RemoveGameObject(BaseObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				if (m_objectMap.Remove(ob.ObjectID) == false)
					throw new Exception();
		}

		public BaseObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");


			lock (m_objectMap)
			{
				BaseObject ob;
				return m_objectMap.TryGetValue(objectID, out ob) ? ob : null;
			}
		}

		public T FindObject<T>(ObjectID objectID) where T : BaseObject
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}


		public BaseObject GetObject(ObjectID objectID)
		{
			var ob = FindObject(objectID);

			if (ob == null)
				throw new Exception();

			return ob;
		}

		public T GetObject<T>(ObjectID objectID) where T : BaseObject
		{
			return (T)GetObject(objectID);
		}

		internal ObjectID GetNewObjectID(ObjectType objectType)
		{
			// XXX overflows
			//return new ObjectID(objectType, Interlocked.Increment(ref m_objectIDcounterArray[(int)objectType]));
			// XXX use a common counter to make debugging simpler
			// XXX check wrapping and int -> uint
			return new ObjectID(objectType, (uint)Interlocked.Increment(ref m_objectIDcounterArray[0]));
		}

		public IEnumerable<BaseObject> AllObjects { get { return m_objectMap.Values.AsEnumerable(); } }

	}
}
