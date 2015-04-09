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
		[SaveGameProperty("Contents")]
		KeyedObjectCollection m_contents;
		public ReadOnlyCollection<MovableObject> Contents { get; private set; }
		IEnumerable<IMovableObject> IContainerObject.Contents { get { return this.Contents; } }

		protected ContainerObject(ObjectType objectType)
			: base(objectType)
		{
			m_contents = new KeyedObjectCollection();
			this.Contents = new ReadOnlyCollection<MovableObject>(m_contents);
		}

		protected ContainerObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
			this.Contents = new ReadOnlyCollection<MovableObject>(m_contents);
		}

		public override void Destruct()
		{
			// Make a copy of the contents
			foreach (var ob in this.Contents.ToArray())
				ob.Destruct();

			base.Destruct();
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			var items = this.Contents.AsEnumerable();

			// filter non-worn and non-wielded if not private visibility
			if ((visibility & ObjectVisibility.Private) == 0)
				items = items.OfType<ItemObject>().Where(o => o.IsEquipped);

			foreach (var o in items)
				o.SendTo(player, visibility);
		}

		internal void RemoveChild(MovableObject ob)
		{
			OnChildRemoved(ob);
			m_contents.Remove(ob);
		}

		internal void AddChild(MovableObject ob)
		{
			m_contents.Add(ob);
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
