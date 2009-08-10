﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MyGame
{
	delegate void HandleChanges(Change[] changes);

	enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}

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
			monster.Name = "monsu";
			monster.MoveTo(world.Map, new IntPoint(2, 2));
			var monsterAI = new MonsterActor(monster);
			monster.Actor = monsterAI;
			TheMonster = monster;

			// Add an item
			var item = new ItemObject(world);
			item.SymbolID = 5;
			item.Name = "testi-itemi";
			item.MoveTo(world.Map, new IntPoint(1, 1));

			// process changes so that moves above are handled
			world.ProcessChanges();
		}



		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter = 0;

		List<Living> m_livingList;

		public event HandleChanges HandleChangesEvent;

		public List<Change> m_changeList = new List<Change>();

		WorldDefinition m_area;
		MapLevel m_map;

		AutoResetEvent m_actorEvent = new AutoResetEvent(false);

		int m_turnNumber = 0;

		public VisibilityMode VisibilityMode { get; private set; }

		public World()
		{
			m_area = new WorldDefinition(this);
			m_map = m_area.GetLevel(1);
			m_map.MapChanged += MapChangedCallback;
			m_livingList = new List<Living>();
			this.VisibilityMode = VisibilityMode.LOS;

			ThreadPool.RegisterWaitForSingleObject(m_actorEvent, Tick, null, -1, false);
		}

		public int TurnNumber
		{
			get { return m_turnNumber; }
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

		public Living[] GetLivings()
		{
			lock (m_livingList)
				return m_livingList.ToArray();
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
					lock(m_changeList)
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
			lock(m_changeList)
				return m_changeList.ToArray();
		}

		/* if changes happen outside the normal Tick processing,
		 * ProcessChanges has to be called manually after that
		 */
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

		public void ForEachObject(Action<ServerGameObject> action)
		{
			foreach (WeakReference weakob in m_objectMap.Values)
			{
				if (weakob.IsAlive && weakob.Target != null)
					action((ServerGameObject)weakob.Target);
			}
		}

	}
}
	