using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	delegate void ObjectMoved(MapLevel e, Location l);

	class ClientGameObject : GameObject
	{
		static Dictionary<ObjectID, WeakReference> s_objectMap = new Dictionary<ObjectID, WeakReference>();

		public static ClientGameObject FindObject(ObjectID objectID)
		{
			if (s_objectMap.ContainsKey(objectID))
			{
				WeakReference weakref = s_objectMap[objectID];
				if (weakref.IsAlive)
					return (ClientGameObject)s_objectMap[objectID].Target;
				else
					s_objectMap.Remove(objectID);
			}

			return null;
		}

		public int SymbolID { get; set; }
		Location m_location;
		MapLevel m_environment;
		LocationGrid<bool> m_visibilityMap;
		public int VisionRange { get; set; }

		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public ClientGameObject(ObjectID objectID)
			: base(objectID)
		{
			s_objectMap.Add(this.ObjectID, new WeakReference(this));
			this.SymbolID = 3;
		}

		public void SetEnvironment(MapLevel map, Location l)
		{
			if (this.Environment != null)
				this.Environment.RemoveObject(this, m_location);

			this.Environment = map;
			m_location = l;

			this.Environment.AddObject(this, m_location);
		}

		public MapLevel Environment
		{
			get
			{
				return m_environment;
			}

			set
			{
				m_environment = value;
			}
		}

		public Location Location
		{
			get
			{
				return m_location;
			}

			set
			{
				this.Environment.RemoveObject(this, m_location);

				m_location = value;

				this.Environment.AddObject(this, m_location);

				if(ObjectMoved != null)
					ObjectMoved(this.Environment, this.Location);
			}
		}

		public LocationGrid<bool> VisibilityMap
		{
			get
			{
				if (m_visibilityMap == null)
					m_visibilityMap = new LocationGrid<bool>(VisionRange*2+1, VisionRange*2+1,
						VisionRange, VisionRange);
				return m_visibilityMap;
			}
		}
	}
}
