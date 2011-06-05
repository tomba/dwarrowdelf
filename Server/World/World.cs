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

	[GameObject(UseRef = true)]
	public partial class World : IWorld
	{
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

		InvokeList m_preTickInvokeList;
		InvokeList m_instantInvokeList;

		[GameProperty]
		Random m_random = new Random();

		Thread m_worldThread;

		public WorldTickMethod TickMethod { get; private set; }

		public World(WorldTickMethod tickMethod)
		{
			this.TickMethod = tickMethod;

			var maxType = Enum.GetValues(typeof(ObjectType)).Cast<int>().Max();
			m_objectIDcounterArray = new int[maxType + 1];

			m_preTickInvokeList = new InvokeList(this);
			m_instantInvokeList = new InvokeList(this);
		}

		public void Initialize(Action initializer)
		{
			EnterWriteLock();

			trace.TraceInformation("Initializing area");
			var m_initSw = Stopwatch.StartNew();

			initializer();

			m_initSw.Stop();
			trace.TraceInformation("Initializing area took {0}", m_initSw.Elapsed);

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
