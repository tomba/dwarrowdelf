using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObject]
	public abstract class MovableObject : ContainerObject, IMovableObject
	{
		[SaveGameProperty]
		public ContainerObject Container { get; private set; }
		IContainerObject IMovableObject.Container { get { return this.Container; } }

		/// <summary>
		/// Return Container as EnvironmentObject
		/// </summary>
		public EnvironmentObject Environment { get { return this.Container as EnvironmentObject; } }
		IEnvironmentObject IMovableObject.Environment { get { return this.Container as IEnvironmentObject; } }

		[SaveGameProperty]
		public IntVector3 Location { get; private set; }
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
			if (this.Container != null)
				MoveToLow(null, new IntVector3());

			base.Destruct();
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (MovableObjectData)baseData;

			data.Container = this.Container != null ? this.Container.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
		}


		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			return props;
		}

		protected virtual void OnParentChanging() { }
		protected virtual void OnParentChanged(ContainerObject src, ContainerObject dst) { }

		protected virtual void OnLocationChanging() { }
		protected virtual void OnLocationChanged() { }

		protected virtual bool OkToMove()
		{
			return true;
		}

		public bool MoveTo(ContainerObject container)
		{
			if (this.Container == container)
				return true;

			return MoveTo(container, new IntVector3());
		}

		public bool MoveTo(ContainerObject dst, IntVector3 dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (!OkToMove())
				return false;

			if (this.Container == dst && this.Location == dstLoc)
				return true;

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			if (dst != this.Container)
				MoveToLow(dst, dstLoc);
			else
				MoveToLow(dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntVector3(x, y, z);
			return MoveTo(p);
		}

		/// <summary>
		/// Move object to given location, without checking if there's a route from current location
		/// </summary>
		public bool MoveTo(IntVector3 location)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Container == null)
				return false;

			if (!OkToMove())
				return false;

			if (this.Location == location)
				return true;

			if (this.Container.OkToAddChild(this, location) == false)
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

			if (LivingExtensions.CanMoveTo(this, dir) == false)
				return false;

			var location = this.Location + dir;

			return MoveTo(location);
		}

		void MoveToLow(ContainerObject dst, IntVector3 dstLoc)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);
			Debug.Assert(this.Container != dst);

			var src = this.Container;
			var srcLoc = this.Location;

			this.OnParentChanging();

			if (src != null)
				src.RemoveChild(this);

			this.Container = dst;
			this.Location = dstLoc;

			if (dst != null)
				dst.AddChild(this);

			this.OnParentChanged(src, dst);

			this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
		}

		void MoveToLow(IntVector3 location)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);
			Debug.Assert(this.Container != null);

			var oldLocation = this.Location;

			if (oldLocation == location)
				return;

			this.OnLocationChanging();

			this.Location = location;

			this.Container.MoveChild(this, oldLocation, location);

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
