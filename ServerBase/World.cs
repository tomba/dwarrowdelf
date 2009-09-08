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
		TurnOngoing,
	}

	public class World
	{
		public IArea Area { get; private set; }
		public IAreaData AreaData { get; private set; }

		// the same single world for everybody, for now
		public static World TheWorld;



		bool m_verbose = false;

		WorldState m_state = WorldState.Idle;

		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter = 0;

		List<Living> m_livingList = new List<Living>();
		List<Living>.Enumerator m_livingEnumerator;
		List<Living> m_addLivingList = new List<Living>();
		List<Living> m_removeLivingList = new List<Living>();

		public event Action<Change[]> HandleChangesEvent;

		List<Change> m_changeList = new List<Change>();

		Environment m_map; // XXX
		public Environment Map 
		{
			get { return m_map; }

			set
			{
				if (m_map != null)
					m_map.MapChanged -= this.MapChangedCallback;

				m_map = value;

				if (m_map != null)
					m_map.MapChanged += this.MapChangedCallback;
			}
		}

		AutoResetEvent m_worldSignal = new AutoResetEvent(false);

		int m_turnNumber = 0;

		WorldTickMethod m_tickMethod = WorldTickMethod.Sequential;

		// maximum time for one living to make its move
		bool m_useMaxMoveTime = false;
		TimeSpan m_maxMoveTime = TimeSpan.FromMilliseconds(1000);
		DateTime m_nextMove = DateTime.MaxValue;

		// minimum time between turns
		bool m_useMinTurnTime = false;
		TimeSpan m_minTurnTime = TimeSpan.FromMilliseconds(2000);
		DateTime m_nextTurn = DateTime.MinValue;

		// Timer is used out-of-turn to start the turn after m_minTurnTime
		// and inside-turn to timeout player movements after m_maxMoveTime
		Timer m_tickTimer;

		// If the user has requested to proceed
		bool m_turnRequested;

		// Require an interactive to be in game for turns to proceed
		bool m_requireInteractive = true;

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
		}

		public void StartWorld()
		{
			Debug.Assert(m_workActive);

			// process any changes from world initialization
			ProcessChanges();

			m_workActive = false;

			ThreadPool.RegisterWaitForSingleObject(m_worldSignal, WorldSignalledWork, null, -1, false);
		}

		public int TurnNumber
		{
			get { return m_turnNumber; }
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

			lock (m_livingList)
			{
				lock (m_addLivingList)
				{
					if (m_addLivingList.Count > 0)
						MyDebug.WriteLine("Processing {0} add livings", m_addLivingList.Count);
					foreach (var living in m_addLivingList)
					{
						Debug.Assert(!m_livingList.Contains(living));
						m_livingList.Add(living);
						living.ActionQueuedEvent += SignalActorStateChanged;
					}

					m_addLivingList.Clear();
				}
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

			lock (m_livingList)
			{
				lock (m_removeLivingList)
				{
					if (m_removeLivingList.Count > 0)
						MyDebug.WriteLine("Processing {0} remove livings", m_removeLivingList.Count);
					foreach (var living in m_removeLivingList)
					{
						living.ActionQueuedEvent -= SignalActorStateChanged;
						bool removed = m_livingList.Remove(living);
						Debug.Assert(removed);
					}

					m_removeLivingList.Clear();
				}
			}
		}


		public Living[] GetLivings()
		{
			Debug.Assert(m_workActive);
			
			lock (m_livingList)
				return m_livingList.ToArray();
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

		// thread safe
		internal void SignalActorStateChanged()
		{
			if (m_verbose)
				MyDebug.WriteLine("SignalActor");
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
			if (m_state != WorldState.Idle)
				return false;

			if (m_useMinTurnTime && DateTime.Now < m_nextTurn)
				return false;

			if (m_requireInteractive && m_turnRequested == false)
				lock (m_livingList)
					if (!m_livingList.Any(l => l.IsInteractive))
						return false;

			return true;
		}

		void Work()
		{
			ProcessInstantInvokeList();

			if (m_state == WorldState.Idle)
			{
				MyDebug.WriteLine("-- Preturn {0} events --", m_turnNumber + 1);

				ProcessInvokeList();
				ProcessAddLivingList();
				ProcessRemoveLivingList();

				MyDebug.WriteLine("-- Preturn {0} events done --", m_turnNumber + 1);

				if (IsTimeToStartTurn())
						StartTurn();
			}

			if (m_state == WorldState.TurnOngoing)
			{
				if (m_tickMethod == WorldTickMethod.Simultaneous)
					SimultaneousWork();
				else if (m_tickMethod == WorldTickMethod.Sequential)
					SequentialWork();
				else
					throw new NotImplementedException();
			}

			ProcessChanges();
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
			Debug.Assert(m_state == WorldState.TurnOngoing);

			lock (m_livingList)
			{
				if (m_livingList.All(l => l.HasAction))
					return true;
			}

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

			lock (m_livingList)
			{
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
			}

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
			AddChange(new TurnChange(m_turnNumber));

			MyDebug.WriteLine("-- Turn {0} started --", m_turnNumber);

			lock (m_livingList)
			{
				m_livingEnumerator = m_livingList.GetEnumerator();
				if (m_livingEnumerator.MoveNext() == false)
					throw new Exception("no livings");
			}

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
			m_state = WorldState.Idle;
		}

		public void AddChange(Change change)
		{
			Debug.Assert(m_workActive);

			//MyDebug.WriteLine("AddChange {0}", change);
			lock(m_changeList)
				m_changeList.Add(change);
		}

		public Change[] GetChanges()
		{
			Debug.Assert(m_workActive);

			lock (m_changeList)
				return m_changeList.ToArray();
		}

		void ProcessChanges()
		{
			Debug.Assert(m_workActive);

			Change[] arr = null;

			lock (m_changeList)
			{
				arr = m_changeList.ToArray();
				m_changeList.Clear();
			}

			if (arr.Length > 0)
			{
				if(HandleChangesEvent != null)
					HandleChangesEvent(arr); // xxx is this needed? perhaps for loggers or something

				foreach (Living living in m_livingList)
				{
					living.ProcessChanges(arr);
				}
			}
		}

		void MapChangedCallback(ObjectID mapID, IntPoint l, int terrainID)
		{
			Debug.Assert(m_workActive);
			// is this needed?
			AddChange(new MapChange(mapID, l, terrainID));
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

		public void ForEachObject(Action<ServerGameObject> action)
		{
			// XXX action can do something that needs objectmap...
			lock (m_objectMap)
			{
				foreach (WeakReference weakob in m_objectMap.Values)
				{
					if (weakob.IsAlive && weakob.Target != null)
						action((ServerGameObject)weakob.Target);
				}
			}
		}
	}
}
	