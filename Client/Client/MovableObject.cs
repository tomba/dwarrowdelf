using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwarrowdelf.Client
{
	delegate void ObjectMoved(MovableObject ob, ContainerObject dst, IntPoint3 loc);

	[SaveGameObject(ClientObject = true)]
	abstract class MovableObject : ContainerObject, IMovableObject
	{
		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public MovableObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
		}

		public override void Destruct()
		{
			MoveTo(null, new IntPoint3());

			base.Destruct();
		}

		public override void ReceiveObjectData(BaseGameObjectData _data)
		{
			var data = (MovableObjectData)_data;

			ContainerObject env = null;
			if (data.Parent != ObjectID.NullObjectID)
				env = this.World.GetObject<ContainerObject>(data.Parent);

			MoveTo(env, data.Location);

			base.ReceiveObjectData(_data);
		}

		public void MoveTo(ContainerObject dst, IntPoint3 dstLoc)
		{
			var src = this.Parent;
			var srcLoc = this.Location;

			if (src != dst)
			{
				if (src != null)
					src.RemoveChild(this);

				this.Parent = dst;
			}

			if (srcLoc != dstLoc)
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

			if (src != dst || srcLoc != dstLoc)
				if (ObjectMoved != null)
					ObjectMoved(this, this.Parent, this.Location);
		}

		public void MoveTo(IntPoint3 location)
		{
			var oldLocation = this.Location;

			this.Location = location;

			this.Parent.MoveChild(this, oldLocation, location);

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		/// <summary>
		/// Return Parent as EnvironmentObject
		/// </summary>
		public EnvironmentObject Environment
		{
			get { return this.Parent as EnvironmentObject; }
		}

		IEnvironmentObject IMovableObject.Environment
		{
			get { return this.Parent as IEnvironmentObject; }
		}

		public override string ToString()
		{
			return String.Format("Object({0})", this.ObjectID);
		}

		ContainerObject m_parent;
		public ContainerObject Parent
		{
			get { return m_parent; }
			private set { m_parent = value; Notify("Parent"); }
		}

		IContainerObject IMovableObject.Parent { get { return this.Parent; } }

		IntPoint3 m_location;
		public IntPoint3 Location
		{
			get { return m_location; }
			private set { m_location = value; Notify("Location"); }
		}
	}
}
