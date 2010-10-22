using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, ServerGameObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(ServerGameObject item)
		{
			return item.ObjectID;
		}
	}

	/* Abstract game object, without inventory or conventional location. */
	abstract public class BaseGameObject : IBaseGameObject
	{
		public ObjectID ObjectID { get; private set; }
		public World World { get; private set; }
		IWorld IBaseGameObject.World { get { return this.World as IWorld; } }

		public bool IsInitialized { get; private set; }
		public bool IsDestructed { get; private set; }

		protected BaseGameObject()
		{
		}

		public virtual void Initialize(World world)
		{
			if (this.IsInitialized)
				throw new Exception();

			this.World = world;
			this.ObjectID = world.GetNewObjectID();

			this.World.AddGameObject(this);
			this.IsInitialized = true;
			this.World.AddChange(new ObjectCreatedChange(this));
		}

		public virtual void Destruct()
		{
			if (!this.IsInitialized)
				throw new Exception();

			this.World.AddChange(new ObjectDestructedChange(this));
			this.IsDestructed = true;
			this.World.RemoveGameObject(this);
		}

		public abstract BaseGameObjectData Serialize();
		public abstract void SerializeTo(Action<Messages.ServerMessage> writer);

		static Dictionary<Type, List<PropertyDefinition>> s_propertyDefinitionMap = new Dictionary<Type, List<PropertyDefinition>>();

		static protected PropertyDefinition RegisterProperty(Type ownerType, PropertyID propertyID, PropertyVisibility visibility, object defaultValue,
			PropertyChangedCallback propertyChangedCallback = null)
		{
			List<PropertyDefinition> propList;

			if (s_propertyDefinitionMap.TryGetValue(ownerType, out propList) == false)
				s_propertyDefinitionMap[ownerType] = new List<PropertyDefinition>();

			Debug.Assert(!s_propertyDefinitionMap[ownerType].Any(p => p.PropertyID == propertyID));

			var prop = new PropertyDefinition(propertyID, visibility, defaultValue, propertyChangedCallback);
			s_propertyDefinitionMap[ownerType].Add(prop);

			return prop;
		}

		Dictionary<PropertyDefinition, object> m_propertyMap = new Dictionary<PropertyDefinition, object>();

		protected void SetValue(PropertyDefinition property, object value)
		{
			Debug.Assert(!this.IsDestructed);

			object oldValue = null;

			if (property.PropertyChangedCallback != null)
				oldValue = GetValue(property);

			m_propertyMap[property] = value;
			if (this.IsInitialized)
				this.World.AddChange(new PropertyChange(this, property, value));

			if (property.PropertyChangedCallback != null)
				property.PropertyChangedCallback(property, this, oldValue, value);
		}

		protected object GetValue(PropertyDefinition property)
		{
			Debug.Assert(!this.IsDestructed);

			object value;
			if (m_propertyMap.TryGetValue(property, out value))
				return value;
			else
				return property.DefaultValue;
		}

		protected Tuple<PropertyID, object>[] SerializeProperties()
		{
			var setProps = m_propertyMap.
				Select(kvp => new Tuple<PropertyID, object>(kvp.Key.PropertyID, kvp.Value));

			var props = setProps;

			var type = GetType();
			do
			{
				if (!s_propertyDefinitionMap.ContainsKey(type))
					continue;

				var defProps = s_propertyDefinitionMap[type].
					Where(pd => !setProps.Any(pp => pd.PropertyID == pp.Item1)).
					Select(pd => new Tuple<PropertyID, object>(pd.PropertyID, pd.DefaultValue));

				props = props.Concat(defProps);

			} while ((type = type.BaseType) != null);

			return props.ToArray();
		}
	}

	/* Game object that has inventory, location */
	abstract public class ServerGameObject : BaseGameObject, IGameObject
	{
		public ServerGameObject Parent { get; private set; }
		public Environment Environment { get { return this.Parent as Environment; } }
		IEnvironment IGameObject.Environment { get { return this.Parent as IEnvironment; } }
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<ServerGameObject> Inventory { get; private set; }

		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		protected ServerGameObject()
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		public override void Destruct()
		{
			this.MoveTo(null);
			base.Destruct();
		}

		static readonly PropertyDefinition NameProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.Name, PropertyVisibility.Public, "");
		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		static readonly PropertyDefinition ColorProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.Color, PropertyVisibility.Public, new GameColor());
		public GameColor Color
		{
			get { return (GameColor)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		static readonly PropertyDefinition SymbolIDProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.SymbolID, PropertyVisibility.Public, SymbolID.Undefined);
		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		static readonly PropertyDefinition MaterialIDProperty = RegisterProperty(typeof(ServerGameObject), PropertyID.MaterialID, PropertyVisibility.Public, MaterialID.Undefined);
		public MaterialID MaterialID
		{
			get { return (MaterialID)GetValue(MaterialIDProperty); }
			set { SetValue(MaterialIDProperty, value); this.Color = Materials.GetMaterial(value).Color; }
		}

		public MaterialClass MaterialClass { get { return Materials.GetMaterial(this.MaterialID).MaterialClass; } } // XXX

		public virtual bool HandleChildAction(ServerGameObject child, GameAction action) { return false; }

		protected virtual bool OkToAddChild(ServerGameObject ob, IntPoint3D dstLoc) { return true; }
		protected virtual bool OkToMoveChild(ServerGameObject ob, Direction dir, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(ServerGameObject child) { }
		protected virtual void OnChildRemoved(ServerGameObject child) { }
		protected virtual void OnChildMoved(ServerGameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc) { }

		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntPoint3D(x, y, z);
			return MoveTo(this.Environment, p);
		}

		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Environment == null)
				throw new Exception();

			var dst = this.Environment;
			var dstLoc = this.Location + dir;

			if (!dst.OkToMoveChild(this, dir, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		void MoveToLow(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			var src = this.Parent;
			var srcLoc = this.Location;

			if (src != dst)
			{
				if (src != null)
				{
					src.OnChildRemoved(this);
					src.m_children.Remove(this);
				}

				this.Parent = dst;
			}

			if (this.Location != dstLoc)
			{
				this.Location = dstLoc;
				if (dst != null && src == dst)
					dst.OnChildMoved(this, srcLoc, dstLoc);
			}

			if (src != dst)
			{
				if (dst != null)
				{
					dst.m_children.Add(this);
					dst.OnChildAdded(this);
				}
			}

			if (src != dst)
				OnEnvironmentChanged(src, dst);

			this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}
}
