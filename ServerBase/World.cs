using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame
{
	delegate void HandleChanges(Change[] changes);

	class World
	{
		// the same single world for everybody, for now
		public static readonly World TheWorld;

		public static readonly Living TheMonster; // XXX

		static World()
		{
			World world = new World();
			TheWorld = world;
			
			// Add a monster
			var monster = new Living(world);
			monster.SymbolID = 4;
			monster.MoveTo(world.Map, new Location(2, 2));
			var monsterAI = new MonsterActor(monster);
			monster.Actor = monsterAI;
			TheMonster = monster;

			// Add an item
			var item = new ItemObject(world);
			item.SymbolID = 5;
			item.MoveTo(world.Map, new Location(1, 1));
		}



		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter = 0;

		List<Living> m_livingList;

		public event HandleChanges ChangesEvent;

		public List<Change> m_changeList = new List<Change>();

		WorldDefinition m_area;
		MapLevel m_map;

		AutoResetEvent m_actorEvent = new AutoResetEvent(false);

		int m_turnNumber = 0;

		public World()
		{
			m_area = new WorldDefinition(this);
			m_map = m_area.GetLevel(1);
			m_map.MapChanged += MapChangedCallback;
			m_livingList = new List<Living>();


			ThreadPool.RegisterWaitForSingleObject(m_actorEvent, Tick, null, -1, false);
		}

		internal void AddLiving(Living living)
		{
			lock (m_livingList)
			{
				Debug.Assert(!m_livingList.Contains(living));
				m_livingList.Add(living);
				living.ActionQueuedEvent += SignalActorStateChanged;
			}
		}

		internal void RemoveLiving(Living living)
		{
			lock (m_livingList)
			{
				living.ActionQueuedEvent -= SignalActorStateChanged;
				bool removed = m_livingList.Remove(living);
				Debug.Assert(removed);
			}
		}

		internal void SignalActorStateChanged()
		{
			MyDebug.WriteLine("SignalActor");
			m_actorEvent.Set();
		}

		void Tick(object state, bool timedOut)
		{
			MyDebug.WriteLine("Tick");
			lock (m_livingList)
			{
				while (true)
				{
					Debug.Assert(m_changeList.Count == 0);

					int count = 0;
					foreach (Living living in m_livingList)
					{
						if (living.HasAction)
							count++;
					}

					if (count != m_livingList.Count)
						break;
					
					// All actors are ready

					ProceedTurn(); // this creates a bunch of changes
					ProcessChanges(); // this sends them to livings, who send them to clients
				}
			}

			MyDebug.WriteLine("Tick done");
		}

		private void ProceedTurn()
		{
			m_turnNumber++;
			AddChange(new TurnChange(m_turnNumber));

			foreach (Living living in m_livingList)
			{
				living.PerformAction();
			}
		}

		public void AddChange(Change change)
		{
			//MyDebug.WriteLine("AddChange {0}", change);
			lock(m_changeList)
				m_changeList.Add(change);
		}

		public Change[] GetChanges()
		{
			return m_changeList.ToArray();
		}

		public void ProcessChanges()
		{
			Change[] arr = null;

			lock (m_changeList)
			{
				arr = m_changeList.ToArray();
				m_changeList.Clear();
			}

			if (arr.Length > 0)
			{
				if(ChangesEvent != null)
					ChangesEvent(arr); // xxx is this needed? perhaps for loggers or something

				foreach (Living living in m_livingList)
				{
					living.ProcessChanges(arr);
				}
			}
		}

		void MapChangedCallback(ObjectID mapID, Location l, int terrainID)
		{
			// is this needed?
			AddChange(new MapChange(mapID, l, terrainID));
		}

		public MapLevel Map
		{
			get { return m_map; }
		}



		public ServerGameObject FindObject(ObjectID objectID)
		{
			if (m_objectMap.ContainsKey(objectID))
			{
				WeakReference weakref = m_objectMap[objectID];
				if (weakref.IsAlive)
					return (ServerGameObject)m_objectMap[objectID].Target;
				else
					m_objectMap.Remove(objectID);
			}

			return null;
		}

		internal void AddGameObject(ServerGameObject ob)
		{
			m_objectMap.Add(ob.ObjectID, new WeakReference(ob));
		}

		internal ObjectID GetNewObjectID()
		{
			return new ObjectID(Interlocked.Increment(ref m_objectIDcounter));
		}

	}
}
	