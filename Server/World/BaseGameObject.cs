using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	/* Abstract game object, without inventory or location. */
	abstract public class BaseGameObject : IBaseGameObject
	{
		[SaveGameProperty]
		public ObjectID ObjectID { get; private set; }
		[SaveGameProperty]
		public World World { get; private set; }
		IWorld IBaseGameObject.World { get { return this.World as IWorld; } }

		[SaveGameProperty]
		public bool IsInitialized { get; private set; }
		[SaveGameProperty]
		public bool IsDestructed { get; private set; }

		public event Action<BaseGameObject> Destructed;

		ObjectType m_objectType;

		protected BaseGameObject(ObjectType objectType)
		{
			m_objectType = objectType;
		}

		protected BaseGameObject(SaveGameContext ctx, ObjectType objectType)
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

		public virtual void SendTo(IPlayer player)
		{
			var data = Serialize();
			var msg = new Messages.ObjectDataMessage() { ObjectData = Serialize() };
			player.Send(msg);
		}

		protected virtual Dictionary<PropertyID, object> SerializeProperties()
		{
			return new Dictionary<PropertyID, object>();
		}

		protected void NotifyObject(PropertyID id, object value)
		{
			Debug.Assert(this.IsInitialized);

			this.World.AddChange(new PropertyObjectChange(this, id, value));
		}

		protected void NotifyInt(PropertyID id, int value)
		{
			Debug.Assert(this.IsInitialized);

			this.World.AddChange(new PropertyIntChange(this, id, value));
		}
	}
}
