using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	/* Abstract game object, without inventory or conventional location. */
	abstract public class BaseGameObject : IBaseGameObject
	{
		static Dictionary<Type, List<PropertyDefinition>> s_propertyDefinitionMap = new Dictionary<Type, List<PropertyDefinition>>();

		static protected PropertyDefinition RegisterProperty(Type ownerType, Type propertyType, PropertyID propertyID, PropertyVisibility visibility, object defaultValue,
			PropertyChangedCallback propertyChangedCallback = null)
		{
			List<PropertyDefinition> propList;

			if (s_propertyDefinitionMap.TryGetValue(ownerType, out propList) == false)
				s_propertyDefinitionMap[ownerType] = new List<PropertyDefinition>();

			Debug.Assert(!s_propertyDefinitionMap[ownerType].Any(p => p.PropertyID == propertyID));

			var prop = new PropertyDefinition(propertyID, propertyType, visibility, defaultValue, propertyChangedCallback);

			s_propertyDefinitionMap[ownerType].Add(prop);

			return prop;
		}

		[GameProperty]
		public ObjectID ObjectID { get; private set; }
		[GameProperty]
		public World World { get; private set; }
		IWorld IBaseGameObject.World { get { return this.World as IWorld; } }

		[GameProperty]
		public bool IsInitialized { get; private set; }
		[GameProperty]
		public bool IsDestructed { get; private set; }

		public event Action<BaseGameObject> Destructed;

		ObjectType m_objectType;

		[GameProperty("PropertyMap", Converter = typeof(PropertyMapConverter))]
		Dictionary<PropertyDefinition, object> m_propertyMap = new Dictionary<PropertyDefinition, object>();

		protected BaseGameObject(ObjectType objectType)
		{
			m_objectType = objectType;
		}

		public virtual void Initialize(World world)
		{
			if (this.IsInitialized)
				throw new Exception();

			if (m_objectType == ObjectType.None)
				throw new Exception();

			this.World = world;
			this.ObjectID = world.GetNewObjectID(m_objectType);

			this.World.AddGameObject(this);
			this.IsInitialized = true;
			this.World.AddChange(new ObjectCreatedChange(this));
		}

		public virtual void Destruct()
		{
			if (!this.IsInitialized)
				throw new Exception();

			if (this.IsDestructed)
				throw new Exception();

			this.IsDestructed = true;

			if (this.Destructed != null)
				this.Destructed(this);

			this.World.AddChange(new ObjectDestructedChange(this));
			this.World.RemoveGameObject(this);
		}

		public abstract BaseGameObjectData Serialize();

		public virtual void SerializeTo(Action<Messages.ClientMessage> writer)
		{
			var msg = new Messages.ObjectDataMessage() { ObjectData = Serialize() };
			writer(msg);
		}

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


		class PropertyMapConverter : Dwarrowdelf.IGameConverter
		{
			public object ConvertToSerializable(object parent, object value)
			{
				var propMap = (Dictionary<PropertyDefinition, object>)value;

				var serializablePropMap = new Dictionary<PropertyID, object>(propMap.Count);

				foreach (var kvp in propMap)
					serializablePropMap[kvp.Key.PropertyID] = kvp.Value;

				return serializablePropMap;
			}

			public object ConvertFromSerializable(object parent, object value)
			{
				BaseGameObject bgo = (BaseGameObject)parent;

				var serializablePropMap = (Dictionary<PropertyID, object>)value;
				var propMap = new Dictionary<PropertyDefinition, object>(serializablePropMap.Count);

				foreach (var kvp in serializablePropMap)
				{
					var type = bgo.GetType();

					do
					{
						if (!s_propertyDefinitionMap.ContainsKey(type))
							continue;

						var propDef = s_propertyDefinitionMap[type].Find(pd => pd.PropertyID == kvp.Key);
						if (propDef != null)
						{
							var v = kvp.Value;

							if (v != null && propDef.PropertyType != v.GetType())
							{
								if (propDef.PropertyType.IsPrimitive)
								{
									v = Convert.ChangeType(v, propDef.PropertyType);
								}
								else
								{
									var conv = System.ComponentModel.TypeDescriptor.GetConverter(propDef.PropertyType);
									v = conv.ConvertFrom(v);
								}
							}

							propMap[propDef] = v;
							break;
						}

					} while ((type = type.BaseType) != null);

					if (type == null)
						throw new Exception("Type owning the property not found");
				}

				return propMap;
			}

			public Type OutputType
			{
				get { return typeof(Dictionary<PropertyID, object>); }
			}
		}
	}
}
