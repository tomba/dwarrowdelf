using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, GameObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(GameObject item)
		{
			return item.ObjectID;
		}
	}

	/* Game object that has inventory, location */
	abstract public class GameObject : BaseGameObject, IGameObject
	{
		[SaveGameProperty]
		public GameObject Parent { get; private set; }
		IGameObject IGameObject.Parent { get { return this.Parent; } }
		public Environment Environment { get { return this.Parent as Environment; } }
		IEnvironment IGameObject.Environment { get { return this.Parent as IEnvironment; } }
		[SaveGameProperty("Inventory")]
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<GameObject> Inventory { get; private set; }

		[SaveGameProperty]
		public IntPoint3D Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		protected GameObject(ObjectType objectType)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<GameObject>(m_children);
		}

		protected GameObject(ObjectType objectType, ServerGameObjectBuilder builder)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<GameObject>(m_children);
		}

		protected GameObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
			this.Inventory = new ReadOnlyCollection<GameObject>(m_children);
		}

		public override void Destruct()
		{
			this.MoveTo(null);

			if (this.Inventory.Count > 0)
			{
				Trace.TraceWarning("{0} contains items when being destructed", this);

				foreach (var ob in this.Inventory.ToArray())
					ob.Destruct();
			}

			base.Destruct();
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			if (visibility == ObjectVisibility.All)
			{
				foreach (var o in this.Inventory)
					o.SendTo(player, visibility);
			}
		}

		protected override void SerializeTo(BaseGameObjectData data, ObjectVisibility visibility)
		{
			base.SerializeTo(data, visibility);

			SerializeToInternal((GameObjectData)data, visibility);
		}

		void SerializeToInternal(GameObjectData data, ObjectVisibility visibility)
		{
			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
		}


		protected override Dictionary<PropertyID, object> SerializeProperties(ObjectVisibility visibility)
		{
			var props = base.SerializeProperties(visibility);
			return props;
		}

		protected virtual bool OkToAddChild(GameObject ob, IntPoint3D dstLoc) { return true; }
		protected virtual bool OkToMoveChild(GameObject ob, Direction dir, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(GameObject child) { }
		protected virtual void OnChildRemoved(GameObject child) { }
		protected virtual void OnChildMoved(GameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc) { }

		protected virtual void OnEnvironmentChanged(GameObject oldEnv, GameObject newEnv) { }

		public bool MoveTo(GameObject parent)
		{
			if (this.Parent == parent)
				return true;

			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(GameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Parent == dst && this.Location == dstLoc)
				return true;

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntPoint3D(x, y, z);
			return MoveTo(p);
		}

		public bool MoveTo(IntPoint3D location)
		{
			if (this.Parent == null)
				return false;

			if (this.Location == location)
				return true;

			if (this.Parent.OkToAddChild(this, location) == false)
				return false;

			MoveToLow(location);

			return true;
		}

		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Environment == null)
				throw new Exception();

			var location = this.Location + dir;

			return MoveTo(location);
		}

		void MoveToLow(GameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			var src = this.Parent;
			var srcLoc = this.Location;

#if DEBUG
			if (src == dst)
				Trace.TraceWarning("MoveToLow(env, pos) shouldn't be used when moving inside one environment");
#endif

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

			if (src == dst)
			{
				this.World.AddChange(new ObjectMoveLocationChange(this, srcLoc, dstLoc));
			}
			else
			{
				OnEnvironmentChanged(src, dst);
				this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
			}
		}

		void MoveToLow(IntPoint3D location)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			var oldLocation = this.Location;

			this.Location = location;
			if (this.Parent != null)
				this.Parent.OnChildMoved(this, oldLocation, location);

			this.World.AddChange(new ObjectMoveLocationChange(this, oldLocation, location));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}

	public abstract class ServerGameObjectBuilder
	{
	}
}
