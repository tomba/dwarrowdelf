using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public abstract class ContainerObject : BaseGameObject, IContainerObject
	{
		[SaveGameProperty("Inventory")]
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<GameObject> Inventory { get; private set; }

		protected ContainerObject(ObjectType objectType)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<GameObject>(m_children);
		}

		protected ContainerObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
			this.Inventory = new ReadOnlyCollection<GameObject>(m_children);
		}

		public override void Destruct()
		{
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

		internal void RemoveChild(GameObject ob)
		{
			OnChildRemoved(ob);
			m_children.Remove(ob);
		}

		internal void AddChild(GameObject ob)
		{
			m_children.Add(ob);
			OnChildAdded(ob);
		}

		internal void MoveChild(GameObject ob, IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			OnChildMoved(ob, srcLoc, dstLoc);
		}

		public virtual bool OkToAddChild(GameObject ob, IntPoint3D dstLoc) { return true; }
		public virtual bool OkToMoveChild(GameObject ob, Direction dir, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(GameObject child) { }
		protected virtual void OnChildRemoved(GameObject child) { }
		protected virtual void OnChildMoved(GameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc) { }
	}
}
