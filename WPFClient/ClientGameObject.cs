using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MyGame
{
	class ObjectCollection : ObservableCollection<ClientGameObject> { }

	class KeyedObjectCollection : ObservableKeyedCollection<ObjectID, ClientGameObject>
	{
		protected override ObjectID GetKeyForItem(ClientGameObject item)
		{
			return item.ObjectID;
		}
	}

	class ReadOnlyKeyedObjectCollection : ReadOnlyObservableKeyedCollection<ObjectID, ClientGameObject>
	{
		public ReadOnlyKeyedObjectCollection(KeyedObjectCollection collection)
			: base(collection)
		{
		}
	}

	delegate void ObjectMoved(ClientGameObject ob, ClientGameObject dst, IntPoint3D loc);

	class ClientGameObject : GameObject, INotifyPropertyChanged
	{
		KeyedObjectCollection m_inventory;
		public ReadOnlyKeyedObjectCollection Inventory { get; private set; }

		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public Color Color { get; set; }
		public ClientGameObject Parent { get; private set; }
		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public bool IsLiving { get; protected set; }
		public World World { get; private set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(objectID)
		{
			this.World = world;
			this.World.AddObject(this);
			m_inventory = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyKeyedObjectCollection(m_inventory);
			this.Color = Colors.Black;
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
