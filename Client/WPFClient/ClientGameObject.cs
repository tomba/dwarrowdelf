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

	abstract class BaseGameObject : DependencyObject, IBaseGameObject
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

	class ClientGameObject : BaseGameObject, IGameObject
	{
		static Dictionary<Tuple<Type, PropertyID>, DependencyProperty> s_propertyMap = new Dictionary<Tuple<Type, PropertyID>, DependencyProperty>();

		static void AddPropertyMapping(Type ownerType, PropertyID propertyID, DependencyProperty dependencyProperty)
		{
			s_propertyMap[Tuple.Create(ownerType, propertyID)] = dependencyProperty;
		}

		protected static DependencyProperty RegisterGameProperty(PropertyID propertyID, string name, Type propertyType, Type ownerType)
		{
			var dprop = DependencyProperty.Register(name, propertyType, ownerType);
			AddPropertyMapping(ownerType, propertyID, dprop);
			return dprop;
		}

		protected static DependencyProperty RegisterGameProperty(PropertyID propertyID, string name, Type propertyType, Type ownerType,
			PropertyMetadata typeMetadata)
		{
			var dprop = DependencyProperty.Register(name, propertyType, ownerType, typeMetadata);
			AddPropertyMapping(ownerType, propertyID, dprop);
			return dprop;
		}

		protected static DependencyProperty RegisterGameProperty(PropertyID propertyID, string name, Type propertyType, Type ownerType,
			PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback)
		{
			var dprop = DependencyProperty.Register(name, propertyType, ownerType, typeMetadata, validateValueCallback);
			AddPropertyMapping(ownerType, propertyID, dprop);
			return dprop;
		}

		protected DependencyProperty GetDependencyProperty(PropertyID propertyID)
		{
			var type = GetType();

			do
			{
				DependencyProperty dprop;

				if (s_propertyMap.TryGetValue(Tuple.Create(type, propertyID), out dprop))
					return dprop;

			} while ((type = type.BaseType) != null);

			throw new Exception();
		}

		GameObjectCollection m_inventory;
		public ReadOnlyGameObjectCollection Inventory { get; private set; }

		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public IBaseGameObject Assignment { get; set; }

		public ClientGameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new GameObjectCollection();
			this.Inventory = new ReadOnlyGameObjectCollection(m_inventory);
		}


		public void SetProperty(PropertyID propertyID, object value)
		{
			var prop = GetDependencyProperty(propertyID);
			SetValue(prop, value);
		}

		public void SetProperties(Tuple<PropertyID, object>[] properties)
		{
			foreach (var tuple in properties)
				SetProperty(tuple.Item1, tuple.Item2);
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
			RegisterGameProperty(PropertyID.Name, "Name", typeof(string), typeof(ClientGameObject), new UIPropertyMetadata(null));




		public GameColor GameColor
		{
			get { return (GameColor)GetValue(GameColorProperty); }
			set { SetValue(GameColorProperty, value); }
		}

		public static readonly DependencyProperty GameColorProperty =
			RegisterGameProperty(PropertyID.Color, "GameColor", typeof(GameColor), typeof(ClientGameObject), new UIPropertyMetadata(GameColor.None, UpdateDrawing));



		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		public static readonly DependencyProperty SymbolIDProperty =
			RegisterGameProperty(PropertyID.SymbolID, "SymbolID", typeof(SymbolID), typeof(ClientGameObject), new UIPropertyMetadata(SymbolID.Undefined, UpdateDrawing));


		public MaterialID MaterialID
		{
			get { return (MaterialID)GetValue(MaterialIDProperty); }
			set { SetValue(MaterialIDProperty, value); }
		}

		public static readonly DependencyProperty MaterialIDProperty =
			RegisterGameProperty(PropertyID.MaterialID, "MaterialID", typeof(MaterialID), typeof(ClientGameObject), new UIPropertyMetadata(MaterialID.Undefined, UpdateMaterial));

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
			ob.Drawing = new DrawingImage(ob.World.SymbolDrawingCache.GetDrawing(ob.SymbolID, ob.GameColor));
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

		IEnvironment IGameObject.Environment
		{
			get { return this.Parent as IEnvironment; }
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (GameObjectData)_data;

			SetProperties(data.Properties);

			ClientGameObject env = null;
			if (data.Environment != ObjectID.NullObjectID)
				env = this.World.FindObject<ClientGameObject>(data.Environment);

			MoveTo(env, data.Location);
		}

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
