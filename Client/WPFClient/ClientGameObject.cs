using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dwarrowdelf.Client
{
	delegate void ObjectMoved(ClientGameObject ob, ClientGameObject dst, IntPoint3D loc);

	abstract class BaseGameObject : IBaseGameObject
	{
		public ObjectID ObjectID { get; private set; }
		public World World { get; private set; }
		IWorld IBaseGameObject.World { get { return this.World as IWorld; } }
		public bool Destructed { get; private set; }

		protected BaseGameObject(World world, ObjectID objectID)
		{
			this.ObjectID = objectID;
			this.World = world;
			this.World.AddObject(this);
		}

		public void Destruct()
		{
			if (this.Destructed)
				throw new Exception();

			this.Destructed = true;
			this.World.RemoveObject(this);
		}

		public abstract void Deserialize(BaseGameObjectData data);
	}

	class ClientGameObject : BaseGameObject, IGameObject, INotifyPropertyChanged
	{
		GameObjectCollection m_inventory;
		public ReadOnlyGameObjectCollection Inventory { get; private set; }

		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public IBaseGameObject ReservedBy { get; set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new GameObjectCollection();
			this.Inventory = new ReadOnlyGameObjectCollection(m_inventory);
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

		IEnvironment IGameObject.Environment
		{
			get { return this.Parent as IEnvironment; }
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (GameObjectData)_data;

			foreach (var tuple in data.Properties)
				SetProperty(tuple.Item1, tuple.Item2);

			ClientGameObject env = null;
			if (data.Environment != ObjectID.NullObjectID)
				env = this.World.FindObject<ClientGameObject>(data.Environment);

			MoveTo(env, data.Location);
		}

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}



		public virtual void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.Name:
					this.Name = (string)value;
					break;

				case PropertyID.Color:
					this.GameColor = (GameColor)value;
					break;

				case PropertyID.SymbolID:
					this.SymbolID = (SymbolID)value;
					break;

				case PropertyID.MaterialID:
					this.MaterialID = (MaterialID)value;
					break;

				default:
					throw new Exception(String.Format("Unknown property {0} in {1}", propertyID, this.GetType().FullName));
			}
		}

		ClientGameObject m_parent;
		public ClientGameObject Parent
		{
			get { return m_parent; }
			private set { m_parent = value; Notify("Parent"); }
		}

		IntPoint3D m_location;
		public IntPoint3D Location
		{
			get { return m_location; }
			private set { m_location = value; Notify("Location"); }
		}

		string m_name;
		public string Name
		{
			get { return m_name; }
			private set { m_name = value; Notify("Name"); }
		}

		GameColor m_gameColor;
		public GameColor GameColor
		{
			get { return m_gameColor; }
			private set
			{
				m_gameColor = value;

				m_drawing = new DrawingImage(this.World.SymbolDrawingCache.GetDrawing(this.SymbolID, this.GameColor));
				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("GameColor");
				Notify("Drawing");
			}
		}

		SymbolID m_symbolID;
		public SymbolID SymbolID
		{
			get { return m_symbolID; }
			private set
			{
				m_symbolID = value;

				m_drawing = new DrawingImage(this.World.SymbolDrawingCache.GetDrawing(this.SymbolID, this.GameColor));
				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("SymbolID");
				Notify("Drawing");
			}
		}

		DrawingImage m_drawing;
		public DrawingImage Drawing
		{
			get { return m_drawing; }
		}


		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			private set
			{
				m_materialID = value;
				m_materialInfo = Materials.GetMaterial(this.MaterialID);
				Notify("MaterialID");
				Notify("Material");
			}
		}

		MaterialInfo m_materialInfo;
		public MaterialInfo Material
		{
			get { return m_materialInfo; }
		}

		public MaterialClass MaterialClass { get { return m_materialInfo.MaterialClass; } } // XXX

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}
}
