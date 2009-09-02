using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MyGame
{
	delegate void ObjectMoved(ServerGameObject o, Environment e, IntPoint l);

	abstract public class ServerGameObject : GameObject
	{
		class KeyedObjectCollection : KeyedCollection<ObjectID, ServerGameObject>
		{
			public KeyedObjectCollection() : base(null, 10) { }

			protected override ObjectID GetKeyForItem(ServerGameObject item)
			{
				return item.ObjectID;
			}
		}

		public int SymbolID { get; set; }

		public string Name { get; set; }

		public ServerGameObject Parent { get; private set; }
		public Environment Environment { get { return this.Parent as Environment; } }
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<ServerGameObject> Inventory { get; private set; }

		public IntPoint Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public GameColor Color { get; set; }

		public World World { get; private set; }

		internal ServerGameObject(World world)
			: base(world.GetNewObjectID())
		{
			this.World = world;
			this.World.AddGameObject(this);
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		protected virtual bool OkToAddChild(ServerGameObject child, IntPoint p) { return true; }
		protected virtual void ChildAdded(ServerGameObject child) { }
		protected virtual void ChildRemoved(ServerGameObject child) { }
		protected virtual void ChildMoved(ServerGameObject child, IntPoint oldLocation, IntPoint newLocation) { }
		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint());
		}

		public bool MoveTo(ServerGameObject parent, IntPoint location)
		{
			ServerGameObject oldParent = this.Parent;
			IntPoint oldLocation = this.Location;

			if (!parent.OkToAddChild(this, location))
				return false;

			if (oldParent != parent)
			{
				if (oldParent != null)
				{
					oldParent.ChildRemoved(this);
					oldParent.m_children.Remove(this);
				}

				this.Parent = parent;
			}

			if (this.Location != location)
			{
				this.Location = location;
				if (oldParent == parent)
					parent.ChildMoved(this, oldLocation, location);
			}

			if (oldParent != parent)
			{
				if (parent != null)
				{
					parent.m_children.Add(this);
					parent.ChildAdded(this);
				}
			}

			if (oldParent != parent)
				OnEnvironmentChanged(oldParent, parent);

			this.World.AddChange(new ObjectMoveChange(this,
				oldParent != null ? oldParent.ObjectID : ObjectID.NullObjectID, oldLocation,
				parent != null ? parent.ObjectID : ObjectID.NullObjectID, location));

			return true;
		}

		public bool MoveDir(Direction dir)
		{
			if (this.Environment == null)
				throw new Exception();

			return MoveTo(this.Environment, this.Location + IntVector.FromDirection(dir));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}
}
