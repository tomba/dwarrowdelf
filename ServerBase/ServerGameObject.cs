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

		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

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

		public abstract ClientMsgs.Message Serialize();

		protected virtual bool OkToAddChild(ServerGameObject child, IntPoint3D p) { return true; }
		protected virtual void ChildAdded(ServerGameObject child) { }
		protected virtual void ChildRemoved(ServerGameObject child) { }
		protected virtual void ChildMoved(ServerGameObject child, IntPoint3D oldLocation, IntPoint3D newLocation) { }
		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ServerGameObject parent, IntPoint3D location)
		{
			ServerGameObject oldParent = this.Parent;
			var oldLocation = this.Location;

			if (parent != null && !parent.OkToAddChild(this, location))
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
				if (parent != null && oldParent == parent)
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

			this.World.AddChange(new ObjectMoveChange(this, oldParent, oldLocation, parent, location));

			return true;
		}

		public bool MoveDir(Direction dir)
		{
			if (this.Environment == null)
				throw new Exception();

			return MoveTo(this.Environment, this.Location + IntVector3D.FromDirection(dir));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}
}
