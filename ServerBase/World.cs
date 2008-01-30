using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		AreaDefinition m_area;
		MapLevel m_map;

		public World()
		{
			this.Dispatcher = new Dispatcher(this, PerformAction);

			m_area = new AreaDefinition();
			m_map = m_area.GetLevel(1);
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

		void PerformAction(GameAction action)
		{
			ServerGameObject ob = FindObject(action.ObjectID);

			if (ob == null)
				throw new Exception("Couldn't find servergameobject");

			if (action is MoveAction)
			{
				MoveAction ma = (MoveAction)action;
				ob.MoveDir(ma.Direction);
			}
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
	