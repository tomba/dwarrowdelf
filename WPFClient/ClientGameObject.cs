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
	}

	class ClientGameObject : BaseGameObject
	{
		static Dictionary<PropertyID, DependencyProperty> s_propertyMap = new Dictionary<PropertyID, DependencyProperty>();
		protected static void AddPropertyMapping(PropertyID propertyID, DependencyProperty dependencyProperty)
		{
			s_propertyMap[propertyID] = dependencyProperty;
		}

		static ClientGameObject()
		{
			AddPropertyMapping(PropertyID.MaterialID, MaterialIDProperty);
			AddPropertyMapping(PropertyID.SymbolID, SymbolIDProperty);
			AddPropertyMapping(PropertyID.Name, NameProperty);
			AddPropertyMapping(PropertyID.Color, GameColorProperty);
		}


		KeyedObjectCollection m_inventory;
		public ReadOnlyKeyedObjectCollection Inventory { get; private set; }

		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public IIdentifiable Assignment { get; set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyKeyedObjectCollection(m_inventory);
			this.Color = Colors.Black;
		}


		public void SetProperty(PropertyID propertyID, object value)
		{
			var prop = s_propertyMap[propertyID];
			SetValue(prop, value);
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

		public GameColor GameColor
		{
			get { return (GameColor)GetValue(GameColorProperty); }
			set { SetValue(GameColorProperty, value); }
		}

		public static readonly DependencyProperty GameColorProperty =
			DependencyProperty.Register("GameColor", typeof(GameColor), typeof(ClientGameObject), new UIPropertyMetadata(GameColors.Black, UpdateColor));

		static void UpdateColor(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ob = (ClientGameObject)d;
			GameColor gc = (GameColor)e.NewValue;
			Color c = gc.ToColor();
			ob.SetValue(ColorProperty, c);
		}


		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		public static readonly DependencyProperty SymbolIDProperty =
			DependencyProperty.Register("SymbolID", typeof(SymbolID), typeof(ClientGameObject), new UIPropertyMetadata(SymbolID.Undefined, UpdateDrawing));


		public MaterialID MaterialID
		{
			get { return (MaterialID)GetValue(MaterialIDProperty); }
			set { SetValue(MaterialIDProperty, value); }
		}

		public static readonly DependencyProperty MaterialIDProperty =
			DependencyProperty.Register("MaterialID", typeof(MaterialID), typeof(ClientGameObject), new UIPropertyMetadata(MaterialID.Undefined, UpdateMaterial));

		static void UpdateMaterial(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ob = (ClientGameObject)d;
			MaterialID matID = (MaterialID)e.NewValue;
			ob.SetValue(MaterialProperty, Materials.GetMaterial(matID));
		}

		public MaterialInfo Material
		{
			get { return (MaterialInfo)GetValue(MaterialProperty); }
		}

		public static readonly DependencyProperty MaterialProperty =
			DependencyProperty.Register("Material", typeof(MaterialInfo), typeof(ClientGameObject), new UIPropertyMetadata(null));



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
