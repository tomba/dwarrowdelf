using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public abstract class MovableObject : ContainerObject, IMovableObject
	{
		[SaveGameProperty]
		public ContainerObject Parent { get; private set; }
		IContainerObject IMovableObject.Parent { get { return this.Parent; } }

		/// <summary>
		/// Return Parent as EnvironmentObject
		/// </summary>
		public EnvironmentObject Environment { get { return this.Parent as EnvironmentObject; } }
		IEnvironmentObject IMovableObject.Environment { get { return this.Parent as IEnvironmentObject; } }

		[SaveGameProperty]
		public IntPoint3 Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		protected MovableObject(ObjectType objectType)
			: base(objectType)
		{
		}

		protected MovableObject(ObjectType objectType, MovableObjectBuilder builder)
			: base(objectType)
		{
		}

		protected MovableObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
		}

		public override void Destruct()
		{
			// use MoveToLow to force the move
			if (this.Parent != null)
				MoveToLow(null, new IntPoint3());

			base.Destruct();
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (MovableObjectData)baseData;

			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
		}


		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			return props;
		}

		protected virtual void OnParentChanging() { }
		protected virtual void OnParentChanged() { }

		protected virtual void OnLocationChanging() { }
		protected virtual void OnLocationChanged() { }

		protected virtual bool OkToMove()
		{
			return true;
		}

		public bool MoveTo(ContainerObject parent)
		{
			if (this.Parent == parent)
				return true;

			return MoveTo(parent, new IntPoint3());
		}

		public bool MoveTo(ContainerObject dst, IntPoint3 dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (!OkToMove())
				return false;

			if (this.Parent == dst && this.Location == dstLoc)
				return true;

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			if (dst != this.Parent)
				MoveToLow(dst, dstLoc);
			else
				MoveToLow(dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntPoint3(x, y, z);
			return MoveTo(p);
		}

		/// <summary>
		/// Move object to given location, without checking if there's a route from current location
		/// </summary>
		public bool MoveTo(IntPoint3 location)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Parent == null)
				return false;

			if (!OkToMove())
				return false;

			if (this.Location == location)
				return true;

			if (this.Parent.OkToAddChild(this, location) == false)
				return false;

			MoveToLow(location);

			return true;
		}

		/// <summary>
		/// Move object to given direction, checking that it is possible to move
		/// </summary>
		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Environment == null)
				throw new Exception();

			if (EnvironmentHelpers.CanMoveFromTo(this, dir) == false)
				return false;

			var location = this.Location + dir;

			return MoveTo(location);
		}

		void MoveToLow(ContainerObject dst, IntPoint3 dstLoc)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);
			Debug.Assert(this.Parent != dst);

			var src = this.Parent;
			var srcLoc = this.Location;

			this.OnParentChanging();

			if (src != null)
				src.RemoveChild(this);

			this.Parent = dst;
			this.Location = dstLoc;

			if (dst != null)
				dst.AddChild(this);

			this.OnParentChanged();

			this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
		}

		void MoveToLow(IntPoint3 location)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);
			Debug.Assert(this.Parent != null);

			var oldLocation = this.Location;

			if (oldLocation == location)
				return;

			this.OnLocationChanging();

			this.Location = location;

			this.Parent.MoveChild(this, oldLocation, location);

			this.OnLocationChanged();

			this.World.AddChange(new ObjectMoveLocationChange(this, oldLocation, location));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}

	public abstract class MovableObjectBuilder
	{
	}
}
