using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	abstract public class MovableObject : ContainerObject, IMovableObject
	{
		[SaveGameProperty]
		public ContainerObject Parent { get; private set; }
		IContainerObject IMovableObject.Parent { get { return this.Parent; } }

		public EnvironmentObject Environment { get { return this.Parent as EnvironmentObject; } }
		IEnvironmentObject IMovableObject.Environment { get { return this.Parent as IEnvironmentObject; } }

		[SaveGameProperty]
		public IntPoint3D Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		protected MovableObject(ObjectType objectType)
			: base(objectType)
		{
		}

		protected MovableObject(ObjectType objectType, ServerGameObjectBuilder builder)
			: base(objectType)
		{
		}

		protected MovableObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
		}

		public override void Destruct()
		{
			this.MoveTo(null);

			base.Destruct();
		}

		protected override void SerializeTo(BaseGameObjectData data, ObjectVisibility visibility)
		{
			base.SerializeTo(data, visibility);

			SerializeToInternal((LocatableGameObjectData)data, visibility);
		}

		void SerializeToInternal(LocatableGameObjectData data, ObjectVisibility visibility)
		{
			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
		}


		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			return props;
		}

		protected virtual void OnEnvironmentChanged(ContainerObject oldEnv, ContainerObject newEnv) { }

		public bool MoveTo(ContainerObject parent)
		{
			if (this.Parent == parent)
				return true;

			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ContainerObject dst, IntPoint3D dstLoc)
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

		void MoveToLow(ContainerObject dst, IntPoint3D dstLoc)
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
					src.RemoveChild(this);

				this.Parent = dst;
			}

			if (this.Location != dstLoc)
			{
				this.Location = dstLoc;
				if (dst != null && src == dst)
					dst.MoveChild(this, srcLoc, dstLoc);
			}

			if (src != dst)
			{
				if (dst != null)
					dst.AddChild(this);
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
				this.Parent.MoveChild(this, oldLocation, location);

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
