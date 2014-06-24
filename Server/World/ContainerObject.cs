using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public abstract class ContainerObject : BaseObject, IContainerObject
	{
		[SaveGameProperty("Inventory")]
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<MovableObject> Inventory { get; private set; }
		IEnumerable<IMovableObject> IContainerObject.Inventory { get { return this.Inventory; } }

		protected ContainerObject(ObjectType objectType)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<MovableObject>(m_children);
		}

		protected ContainerObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
			this.Inventory = new ReadOnlyCollection<MovableObject>(m_children);
		}

		public override void Destruct()
		{
			// Make a copy of the inventory
			foreach (var ob in this.Inventory.ToArray())
				ob.Destruct();

			base.Destruct();
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			var items = this.Inventory.AsEnumerable();

			// filter non-worn and non-wielded if not private visibility
			if ((visibility & ObjectVisibility.Private) == 0)
				items = items.OfType<ItemObject>().Where(o => o.IsEquipped);

			foreach (var o in items)
				o.SendTo(player, visibility);
		}

		internal void RemoveChild(MovableObject ob)
		{
			OnChildRemoved(ob);
			m_children.Remove(ob);
		}

		internal void AddChild(MovableObject ob)
		{
			m_children.Add(ob);
			OnChildAdded(ob);
		}

		internal void MoveChild(MovableObject ob, IntVector3 srcLoc, IntVector3 dstLoc)
		{
			OnChildMoved(ob, srcLoc, dstLoc);
		}

		public virtual bool OkToAddChild(MovableObject ob, IntVector3 dstLoc) { return true; }
		public virtual bool OkToMoveChild(MovableObject ob, Direction dir, IntVector3 dstLoc) { return true; }

		protected virtual void OnChildAdded(MovableObject child) { }
		protected virtual void OnChildRemoved(MovableObject child) { }
		protected virtual void OnChildMoved(MovableObject child, IntVector3 srcLoc, IntVector3 dstLoc) { }
	}
}
