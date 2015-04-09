using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	public abstract class ContainerObject : BaseObject, IContainerObject
	{
		MovableObjectCollection m_contents;
		public ReadOnlyMovableObjectCollection Contents { get; private set; }
		IEnumerable<IMovableObject> IContainerObject.Contents { get { return this.Contents; } }

		public ContainerObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_contents = new MovableObjectCollection();
			this.Contents = new ReadOnlyMovableObjectCollection(m_contents);
		}

		protected virtual void ChildAdded(MovableObject child) { }
		protected virtual void ChildRemoved(MovableObject child) { }
		protected virtual void ChildMoved(MovableObject child, IntVector3 from, IntVector3 to) { }

		public void AddChild(MovableObject ob)
		{
			m_contents.Add(ob);
			ChildAdded(ob);
		}

		public void RemoveChild(MovableObject ob)
		{
			m_contents.Remove(ob);
			ChildRemoved(ob);
		}

		public void MoveChild(MovableObject ob, IntVector3 from, IntVector3 to)
		{
			ChildMoved(ob, from, to);
		}
	}
}
