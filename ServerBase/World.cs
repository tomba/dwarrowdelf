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

			var monster = new ServerGameObject(world);
			monster.MoveTo(world.Map, new Location(2, 5));
			var monsterAI = new MonsterActor(monster);
			world.AddActor(monsterAI);
		}

		public event HandleChanges ChangesEvent;

		public List<Change> m_changeList = new List<Change>();

		List<IActor> m_actorList;

		AreaDefinition m_area;
		MapLevel m_map;

		AutoResetEvent m_actorEvent = new AutoResetEvent(false);

		int m_turnNumber = 0;

		public World()
		{
			m_area = new AreaDefinition(this);
			m_map = m_area.GetLevel(1);
			m_actorList = new List<IActor>();

			ThreadPool.RegisterWaitForSingleObject(m_actorEvent, Tick, null, -1, false);
		}

		internal void AddActor(IActor actor)
		{
			lock (m_actorList)
			{
				Debug.Assert(!m_actorList.Contains(actor));
				m_actorList.Add(actor);
				actor.ActionQueuedEvent += SignalActorStateChanged;
			}
		}

		internal void RemoveActor(IActor actor)
		{
			lock (m_actorList)
			{
				actor.ActionQueuedEvent -= SignalActorStateChanged;
				bool removed = m_actorList.Remove(actor);
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
			lock (m_actorList)
			{
				while (true)
				{
					
					int count = 0;
					foreach (IActor ob in m_actorList)
					{
						if (ob.HasAction)
							count++;
					}

					if (count != m_actorList.Count)
						break;
					
					// All actors are ready

					ProceedTurn();
				}

				SendChanges();
			}

			MyDebug.WriteLine("Tick done");
		}

		private void ProceedTurn()
		{
			m_turnNumber++;
			AddChange(new TurnChange(m_turnNumber));

			foreach (IActor ob in m_actorList)
			{
				GameAction action = ob.PeekAction();
				// if action was cancelled just now, the actor misses the turn
				if (action == null)
					continue;

				bool done = PerformAction(action);

				if (done)
					ob.DequeueAction();
			}
		}


		public void AddChange(Change change)
		{
			//MyDebug.WriteLine("AddChange {0}", change);
			lock(m_changeList)
				m_changeList.Add(change);
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

		bool PerformAction(GameAction action)
		{
			ServerGameObject ob = FindObject(action.ObjectID);

			if (ob == null)
				throw new Exception("Couldn't find servergameobject");

			if (action is MoveAction)
			{
				MoveAction ma = (MoveAction)action;
				ob.MoveDir(ma.Direction);
				return true;
			}
			else if (action is WaitAction)
			{
				WaitAction wa = (WaitAction)action;
				wa.Turns--;
				if (wa.Turns == 0)
					return true;
				else 
					return false;
			}
			else
				throw new NotImplementedException();
		}


		public MapLevel Map
		{
			get { return m_map; }
		}



		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();

		int m_objectIDcounter = 0;

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
	