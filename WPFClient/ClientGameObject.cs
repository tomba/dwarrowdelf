using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows;

namespace MyGame.Client
{
	class ObjectCollection : ObservableCollection<ClientGameObject> { }

	class KeyedObjectCollection : ObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		protected override ObjectID GetKeyForItem(IIdentifiable item)
		{
			return item.ObjectID;
		}
	}

	class ReadOnlyKeyedObjectCollection : ReadOnlyObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		public ReadOnlyKeyedObjectCollection(KeyedObjectCollection collection)
			: base(collection)
		{
		}
	}

	delegate void ObjectMoved(ClientGameObject ob, ClientGameObject dst, IntPoint3D loc);

	abstract class BaseGameObject : DependencyObject, IIdentifiable
	{
		public ObjectID ObjectID { get; private set; }
		public World World { get; private set; }

		protected BaseGameObject(World world, ObjectID objectID)
		{
			this.ObjectID = objectID;
			this.World = world;
			this.World.AddObject(this);
		}
	}

	class ClientGameObject : BaseGameObject
	{
		KeyedObjectCollection m_inventory;
		public ReadOnlyKeyedObjectCollection Inventory { get; private set; }

		public event ObjectMoved ObjectMoved;

		public MaterialInfo Material { get; set; }

		public bool IsLiving { get; protected set; }

		public bool Destructed { get; private set; }

		public IIdentifiable Assignment { get; set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyKeyedObjectCollection(m_inventory);
			this.Color = Colors.Black;
		}

		public void Destruct()
		{
			if (this.Destructed)
				return;

			this.Destructed = true;
			this.World.RemoveObject(this);
		}




		public ClientGameObject Parent
		{
			get { return (ClientGameObject)GetValue(ParentProperty); }
			private set { SetValue(ParentProperty, value); }
		}

		public static readonly DependencyProperty ParentProperty =
			DependencyProperty.Register("Parent", typeof(ClientGameObject), typeof(ClientGameObject), new UIPropertyMetadata(null));



		public IntPoint3D Location
		{
			get { return (IntPoint3D)GetValue(LocationProperty); }
			private set { SetValue(LocationProperty, value); }
		}

		public static readonly DependencyProperty LocationProperty =
			DependencyProperty.Register("Location", typeof(IntPoint3D), typeof(ClientGameObject), new UIPropertyMetadata(new IntPoint3D()));




		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		public static readonly DependencyProperty NameProperty =
			DependencyProperty.Register("Name", typeof(string), typeof(ClientGameObject), new UIPropertyMetadata(null));




		public Color Color
		{
			get { return (Color)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		public static readonly DependencyProperty ColorProperty =
			DependencyProperty.Register("Color", typeof(Color), typeof(ClientGameObject), new UIPropertyMetadata(Colors.Black, UpdateDrawing));



		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		public static readonly DependencyProperty SymbolIDProperty =
			DependencyProperty.Register("SymbolID", typeof(SymbolID), typeof(ClientGameObject), new UIPropertyMetadata(SymbolID.Undefined, UpdateDrawing));




		public DrawingImage Drawing
		{
			get { return (DrawingImage)GetValue(DrawingProperty); }
			set { SetValue(DrawingProperty, value); }
		}

		public static readonly DependencyProperty DrawingProperty =
			DependencyProperty.Register("Drawing", typeof(DrawingImage), typeof(ClientGameObject), new UIPropertyMetadata(null));

		static void UpdateDrawing(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ob = (ClientGameObject)d;
			ob.Drawing = new DrawingImage(ob.World.SymbolDrawingCache.GetDrawing(ob.SymbolID, ob.Color));
			if (ob.Environment != null)
				ob.Environment.OnObjectVisualChanged(ob);
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

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
