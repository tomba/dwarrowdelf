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

		static World()
		{
			World world = new World();
			TheWorld = world;
			
			var monster = new Living(world);
			monster.MoveTo(world.Map, new Location(2, 5));
			var monsterAI = new MonsterActor(monster);
			monster.Actor = monsterAI;
			 
		}



		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();
		int m_objectIDcounter = 0;

		List<Living> m_livingList;

		public event HandleChanges ChangesEvent;

		public List<Change> m_changeList = new List<Change>();

		AreaDefinition m_area;
		MapLevel m_map;

		AutoResetEvent m_actorEvent = new AutoResetEvent(false);

		int m_turnNumber = 0;

		public World()
		{
			m_area = new AreaDefinition(this);
			m_map = m_area.GetLevel(1);
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
					
					int count = 0;
					foreach (Living living in m_livingList)
					{
						if (living.HasAction)
							count++;
					}

					if (count != m_livingList.Count)
						break;
					
					// All actors are ready

					ProceedTurn();

					PostTurn();

					SendChanges();
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

		public void PostTurn()
		{
			foreach (Living living in m_livingList)
			{
				living.PostTurn();
			}
		}

		public void AddChange(Change change)
		{
			//MyDebug.WriteLine("AddChange {0}", change);
			lock(m_changeList)
				m_changeList.Add(change);

			SendChanges(); // xxx we send every change immediately, to keep the order of the messages. sigh.
		}

		public void SendChanges()
		{
			Change[] arr = null;

			lock (m_changeList)
			{
				if (ChangesEvent != null)
					arr = m_changeList.ToArray();
				m_changeList.Clear();
			}

			if(arr != null && arr.Length > 0)
				ChangesEvent(arr);
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
	