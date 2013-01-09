using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	public abstract class ContainerObject : BaseObject, IContainerObject
	{
		MovableObjectCollection m_inventory;
		public ReadOnlyMovableObjectCollection Inventory { get; private set; }
		IEnumerable<IMovableObject> IContainerObject.Inventory { get { return this.Inventory; } }

		public ContainerObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new MovableObjectCollection();
			this.Inventory = new ReadOnlyMovableObjectCollection(m_inventory);
		}

		protected virtual void ChildAdded(MovableObject child) { }
		protected virtual void ChildRemoved(MovableObject child) { }
		protected virtual void ChildMoved(MovableObject child, IntPoint3 from, IntPoint3 to) { }

		public void AddChild(MovableObject ob)
		{
			m_inventory.Add(ob);
			ChildAdded(ob);
		}

		public void RemoveChild(MovableObject ob)
		{
			m_inventory.Remove(ob);
			ChildRemoved(ob);
		}

		public void MoveChild(MovableObject ob, IntPoint3 from, IntPoint3 to)
		{
			ChildMoved(ob, from, to);
		}
	}
}
