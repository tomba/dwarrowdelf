using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	/* Abstract game object, without inventory or location. */
	[SaveGameObject]
	public abstract class BaseObject : IBaseObject
	{
		[SaveGameProperty]
		public ObjectID ObjectID { get; private set; }

		public ObjectType ObjectType { get { return this.ObjectID.ObjectType; } }

		[SaveGameProperty]
		public DateTime CreationTime { get; private set; }

		[SaveGameProperty]
		public int CreationTick { get; private set; }

		[SaveGameProperty]
		public World World { get; private set; }
		IWorld IBaseObject.World { get { return this.World; } }

		[SaveGameProperty]
		public bool IsInitialized { get; private set; }
		[SaveGameProperty]
		public bool IsDestructed { get; private set; }

		public event Action<IBaseObject> Destructed;

		ObjectType m_objectType;

		protected BaseObject(ObjectType objectType)
		{
			m_objectType = objectType;
		}

		protected BaseObject(SaveGameContext ctx, ObjectType objectType)
			: this(objectType)
		{
		}

		protected virtual void Initialize(World world)
		{
			if (this.IsInitialized)
				throw new Exception();

			if (m_objectType == ObjectType.None)
				throw new Exception();

			this.World = world;
			this.ObjectID = world.GetNewObjectID(m_objectType);

			this.CreationTime = DateTime.Now;
			this.CreationTick = this.World.TickNumber;

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

		public abstract void SendTo(IPlayer player, ObjectVisibility visibility);

		protected virtual void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			baseData.ObjectID = this.ObjectID;

			baseData.CreationTime = this.CreationTime;
			baseData.CreationTick = this.CreationTick;

			baseData.Properties = SerializeProperties().
				Where(kvp => (PropertyVisibilities.GetPropertyVisibility(kvp.Key) & visibility) != 0).
				Select(kvp => new Tuple<PropertyID, object>(kvp.Key, kvp.Value)).
				ToArray();
		}

		protected virtual Dictionary<PropertyID, object> SerializeProperties()
		{
			Debug.Assert(!this.IsDestructed);
			return new Dictionary<PropertyID, object>();
		}

		protected void NotifyValue(PropertyID id, ValueType value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(value.GetType().IsValueType);
			Debug.Assert(!this.IsDestructed);

			this.World.AddChange(new PropertyValueChange(this, id, value));
		}

		protected void NotifyBool(PropertyID id, bool value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			this.World.AddChange(new PropertyValueChange(this, id, value));
		}

		protected void NotifyInt(PropertyID id, int value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			this.World.AddChange(new PropertyIntChange(this, id, value));
		}

		protected void NotifyString(PropertyID id, string value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			this.World.AddChange(new PropertyStringChange(this, id, value));
		}
	}
}
