﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MyGame
{
	delegate void ObjectMoved(MapLevel e, IntPoint l);
	class ItemCollection : ObservableCollection<ClientGameObject> { }

	class ClientGameObject : GameObject, INotifyPropertyChanged
	{
		// XXX not re-entrant
		static LOSAlgo s_losAlgo = new LOSShadowCast1();

		static Dictionary<ObjectID, WeakReference> s_objectMap = new Dictionary<ObjectID, WeakReference>();

		static void AddObject(ClientGameObject ob)
		{
			lock (s_objectMap)
				s_objectMap.Add(ob.ObjectID, new WeakReference(ob));
			GameData.Data.Objects = null;
		}

		public static ClientGameObject FindObject(ObjectID objectID)
		{
			lock (s_objectMap)
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
		}

		public static T FindObject<T>(ObjectID objectID) where T : ClientGameObject
		{
			lock (s_objectMap)
			{
				if (s_objectMap.ContainsKey(objectID))
				{
					WeakReference weakref = s_objectMap[objectID];
					if (weakref.IsAlive)
						return (T)s_objectMap[objectID].Target;
					else
						s_objectMap.Remove(objectID);
				}

				return default(T);
			}
		}
		
		public static ClientGameObject[] GetObjects()
		{
			lock (s_objectMap)
			{
				return s_objectMap.
					Where(kvp => kvp.Value.IsAlive).
					Select(kvp => (ClientGameObject)kvp.Value.Target).
					ToArray();
			}
		}
		
		public ItemCollection Inventory { get; private set; }
		IntPoint m_location;
		MapLevel m_environment;

		IntPoint m_visibilityLocation;
		LocationGrid<bool> m_visibilityMap;

		int m_visionRange;
		public int VisionRange
		{
			get { return m_visionRange; }
			set { m_visionRange = value; m_visibilityMap = null; }
		}

		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public ClientGameObject(ObjectID objectID)
			: base(objectID)
		{
			this.Inventory = new ItemCollection();
			AddObject(this);
		}

		string m_name;
		public string Name {
			get { return m_name; }
			set { m_name = value; Notify("Name"); }
		}

		int m_symbolID;
		public int SymbolID
		{
			get { return m_symbolID; }
			set
			{
				m_symbolID = value;
				Notify("SymbolID");
				Notify("Drawing");
			}
		}

		public DrawingImage Drawing
		{
			get
			{
				return new DrawingImage(GameData.Data.SymbolDrawings[this.SymbolID]);
			}
		}

		public void SetEnvironment(MapLevel map, IntPoint l)
		{
			if (this.Environment != null)
				this.Environment.RemoveObject(this, m_location);

			m_environment = map;
			m_location = l;

			this.Environment.AddObject(this, m_location);
		}

		public MapLevel Environment
		{
			get { return m_environment; }
		}

		public IntPoint Location
		{
			get { return m_location; }

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
				bool update = m_visibilityLocation != m_location;

				if (m_visibilityMap == null)
				{
					m_visibilityMap = new LocationGrid<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1,
						m_visionRange, m_visionRange);
					update = true;
				}

				if (update && this.Environment != null)
				{
					s_losAlgo.Calculate(m_location, m_visionRange,
						m_visibilityMap, this.Environment.Bounds,
						(IntPoint l) => { return this.Environment.GetTerrainType(l) == 2; });
					m_visibilityLocation = m_location;
				}

				return m_visibilityMap;
			}
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
