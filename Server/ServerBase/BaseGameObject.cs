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

		Dictionary<PropertyDefinition, object> m_propertyMap = new Dictionary<PropertyDefinition, object>();
		[GameProperty("PropertyMap")]
		Dictionary<PropertyID, object> m_serializablePropertyMap;

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

		[OnGameSerializing]
		void OnSerializing()
		{
			m_serializablePropertyMap = new Dictionary<PropertyID, object>();

			foreach (var kvp in m_propertyMap)
				m_serializablePropertyMap[kvp.Key.PropertyID] = kvp.Value;
		}

		[OnGameSerialized]
		void OnSerialized()
		{
			m_serializablePropertyMap = null;
		}

		[OnGameDeserialized]
		void OnDeserialized()
		{
			foreach (var kvp in m_serializablePropertyMap)
			{
				var type = this.GetType();

				do
				{
					if (!s_propertyDefinitionMap.ContainsKey(type))
						continue;

					var propDef = s_propertyDefinitionMap[type].Find(pd => pd.PropertyID == kvp.Key);
					if (propDef != null)
					{
						var value = kvp.Value;
						if (propDef.PropertyType.IsEnum && value.GetType() == typeof(string))
						{
							var conv = System.ComponentModel.TypeDescriptor.GetConverter(propDef.PropertyType);
							value = conv.ConvertFrom(value);
						}

						m_propertyMap[propDef] = value;
						break;
					}

				} while ((type = type.BaseType) != null);

				if (type == null)
					throw new Exception("Type owning the property not found");
			}

			m_serializablePropertyMap = null;
		}

		public abstract BaseGameObjectData Serialize();
		public abstract void SerializeTo(Action<Messages.ServerMessage> writer);

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
}
