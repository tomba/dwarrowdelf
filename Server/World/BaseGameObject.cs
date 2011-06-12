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
	}
}
