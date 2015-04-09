using System;
using System.Collections.Generic;
using System.Linq;

namespace Dwarrowdelf.Client
{
	public delegate void ObjectMoved(MovableObject ob, ContainerObject dst, IntVector3 loc);

	[SaveGameObject(ClientObject = true)]
	public abstract class MovableObject : ContainerObject, IMovableObject
	{
		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public MovableObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
		}

		public override void Destruct()
		{
			MoveTo(null, new IntVector3());

			base.Destruct();
		}

		public override void ReceiveObjectData(BaseGameObjectData _data)
		{
			var data = (MovableObjectData)_data;

			ContainerObject env = null;
			if (data.Container != ObjectID.NullObjectID)
				env = this.World.GetObject<ContainerObject>(data.Container);

			MoveTo(env, data.Location);

			base.ReceiveObjectData(_data);
		}

		public void MoveTo(ContainerObject dst, IntVector3 dstLoc)
		{
			var src = this.Container;
			var srcLoc = this.Location;

			if (src != dst)
			{
				if (src != null)
					src.RemoveChild(this);

				this.Container = dst;
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
					ObjectMoved(this, this.Container, this.Location);
		}

		public void MoveTo(IntVector3 location)
		{
			var oldLocation = this.Location;

			this.Location = location;

			this.Container.MoveChild(this, oldLocation, location);

			if (ObjectMoved != null)
				ObjectMoved(this, this.Container, this.Location);
		}

		/// <summary>
		/// Return Container as EnvironmentObject
		/// </summary>
		public EnvironmentObject Environment
		{
			get { return this.Container as EnvironmentObject; }
		}

		IEnvironmentObject IMovableObject.Environment
		{
			get { return this.Container as IEnvironmentObject; }
		}

		public override string ToString()
		{
			return String.Format("Object({0})", this.ObjectID);
		}

		ContainerObject m_container;
		public ContainerObject Container
		{
			get { return m_container; }
			private set { m_container = value; Notify("Container"); }
		}

		IContainerObject IMovableObject.Container { get { return this.Container; } }

		IntVector3 m_location;
		public IntVector3 Location
		{
			get { return m_location; }
			private set { m_location = value; Notify("Location"); }
		}
	}
}
