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
		[ThreadStatic]
		public static World CurrentWorld;

		public event HandleChanges ChangesEvent;

		public Dispatcher Dispatcher { get; private set; }
		public List<Change> m_changeList = new List<Change>();

		List<IActor> m_actorList;

		AreaDefinition m_area;
		MapLevel m_map;

		AutoResetEvent m_actorEvent = new AutoResetEvent(false);

		public World()
		{
			//this.Dispatcher = new Dispatcher(this, PerformAction);

			m_area = new AreaDefinition(this);
			m_map = m_area.GetLevel(1);
			m_actorList = new List<IActor>();

			ThreadPool.RegisterWaitForSingleObject(m_actorEvent, Tick, null, -1, true);
		}

		internal void AddActor(IActor actor)
		{
			lock (m_actorList)
			{
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
			Debug.WriteLine("SignalActor");
			m_actorEvent.Set();
		}

		void Tick(object state, bool timedOut)
		{
			Debug.WriteLine("Tick");
			lock (m_actorList)
			{
				while (true)
				{
					int count = 0;
					foreach (IActor ob in m_actorList)
					{
						if (ob.PeekAction() != null)
							count++;
					}

					if (count != m_actorList.Count)
						break;

					// All actors are ready

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

				SendChanges();
			}
			ThreadPool.RegisterWaitForSingleObject(m_actorEvent, Tick, null, -1, true);
			Debug.WriteLine("Tick done");
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

		public MapLevel Map
		{
			get { return m_map; }
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



		Dictionary<ObjectID, WeakReference> m_objectMap = new Dictionary<ObjectID, WeakReference>();

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

		int m_objectIDcounter = 1;

		internal ObjectID GetNewObjectID()
		{
			return new ObjectID(m_objectIDcounter++);
		}

	}
}
	