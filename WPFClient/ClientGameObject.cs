using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;

namespace MyGame
{
	class ObservableObjectCollection : ObservableKeyedCollection<ObjectID, ClientGameObject>
	{
		protected override ObjectID GetKeyForItem(ClientGameObject item)
		{
			return item.ObjectID;
		}
	}

	class ReadOnlyObservableObjectCollection : ReadOnlyObservableKeyedCollection<ObjectID, ClientGameObject>
	{
		public ReadOnlyObservableObjectCollection(ObservableObjectCollection collection)
			: base(collection)
		{
		}
	}

	delegate void ObjectMoved(ClientGameObject ob, ClientGameObject dst, IntPoint3D loc);

	class ClientGameObject : GameObject, INotifyPropertyChanged
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		ObservableObjectCollection m_inventory;
		public ReadOnlyObservableObjectCollection Inventory { get; private set; }

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		int m_visionRange;
		Grid2D<bool> m_visionMap;

		public int VisionRange
		{
			get { return m_visionRange; }
			set { m_visionRange = value; m_visionMap = null; }
		}

		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public Color Color { get; set; }
		public ClientGameObject Parent { get; private set; }
		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public bool IsLiving { get; set; }
		public World World { get; private set; }

		public AI AI { get; private set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(objectID)
		{
			this.World = world;
			this.World.AddObject(this);
			m_inventory = new ObservableObjectCollection();
			this.Inventory = new ReadOnlyObservableObjectCollection(m_inventory);
			this.Color = Colors.Black;
			this.AI = new AI(this);
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
				return new DrawingImage(this.World.SymbolDrawings.GetDrawing(m_symbolID, this.Color));
			}
		}

		protected virtual void ChildAdded(ClientGameObject child) { }
		protected virtual void ChildRemoved(ClientGameObject child) { }

		public void MoveTo(ClientGameObject parent, IntPoint3D location)
		{
			ClientGameObject oldParent = this.Parent;

			if (oldParent != null)
			{
				oldParent.m_inventory.Remove(this);
				oldParent.ChildRemoved(this);
			}

			this.Parent = parent;
			this.Location = location;

			if (parent != null)
			{
				parent.m_inventory.Add(this);
				parent.ChildAdded(this);
			}

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		public Environment Environment
		{
			get { return this.Parent as Environment; }
		}

		public Grid2D<bool> VisionMap
		{
			get
			{
				UpdateLOS();
				return m_visionMap;
			}
		}

		void UpdateLOS()
		{
			Debug.Assert(this.Environment.VisibilityMode == VisibilityMode.LOS);

			if (this.Environment == null)
				return;

			if (m_losLocation == this.Location && m_losMapVersion == this.Environment.Version)
				return;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1,
					m_visionRange, m_visionRange);
				m_losMapVersion = 0;
			}

			var terrains = this.Environment.World.AreaData.Terrains;
			var level = this.Environment.GetLevel(this.Location.Z);

			s_losAlgo.Calculate(this.Location2D, m_visionRange,
				m_visionMap, level.Bounds,
				l => terrains[level.GetTerrainID(l)].IsWalkable == false);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
