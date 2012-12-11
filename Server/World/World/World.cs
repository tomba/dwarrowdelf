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
		}

		World(SaveGameContext ctx)
			: this()
		{
			if (this.TickMethod == WorldTickMethod.Sequential)
				m_livingEnumerator = new LivingEnumerator(m_livings.List);
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

			if (this.TickMethod == WorldTickMethod.Sequential)
				m_livingEnumerator = new LivingEnumerator(m_livings.List);

			this.Year = 1;
			this.YearOctant = 1;
			this.Season = (GameSeason)((this.YearOctant + 7) / 2 % 4);
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
	}
}
