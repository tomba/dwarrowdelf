using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MyGame
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, ServerGameObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(ServerGameObject item)
		{
			return item.ObjectID;
		}
	}

	abstract public class ServerGameObject : GameObject
	{
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
		public EnvPos EnvLoc { get { return new EnvPos(this.Parent, this.Location); } }

		public GameColor Color { get; set; }
		public MaterialID MaterialID { get; set; }

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

		public virtual bool HandleChildAction(ServerGameObject child, GameAction action) { return false; }

		protected virtual bool OkToAddChild(ServerGameObject ob, IntPoint3D dstLoc) { return true; }
		protected virtual bool OkToMoveChild(ServerGameObject ob, IntVector3D dirVec, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(ServerGameObject child) { }
		protected virtual void OnChildRemoved(ServerGameObject child) { }
		protected virtual void OnChildMoved(ServerGameObject child, IntPoint3D oldLocation, IntPoint3D newLocation) { }

		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.World.IsWriteable);

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWriteable);

			if (this.Environment == null)
				throw new Exception();

			var dirVec = IntVector3D.FromDirection(dir);
			var dst = this.Environment;
			var dstLoc = this.Location + dirVec;

			if (!dst.OkToMoveChild(this, dirVec, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		void MoveToLow(ServerGameObject dst, IntPoint3D dstLoc)
		{
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
