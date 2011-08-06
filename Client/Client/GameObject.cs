using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dwarrowdelf.Client
{
	delegate void ObjectMoved(GameObject ob, GameObject dst, IntPoint3D loc);

	abstract class GameObject : BaseGameObject, IGameObject
	{
		GameObjectCollection m_inventory;
		public ReadOnlyGameObjectCollection Inventory { get; private set; }

		public event ObjectMoved ObjectMoved;

		public bool IsLiving { get; protected set; }

		public GameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_inventory = new GameObjectCollection();
			this.Inventory = new ReadOnlyGameObjectCollection(m_inventory);
		}

		protected virtual void ChildAdded(GameObject child) { }
		protected virtual void ChildRemoved(GameObject child) { }
		protected virtual void ChildMoved(GameObject child, IntPoint3D from, IntPoint3D to) { }

		public void MoveTo(GameObject parent, IntPoint3D location)
		{
			GameObject oldParent = this.Parent;

			if (oldParent != null)
			{
				oldParent.m_inventory.Remove(this);
				oldParent.ChildRemoved(this);
			}

			this.Parent = parent;
			this.Location = location;

			if (parent != null)
			{
				parent.m_inventory.Add(this);
				parent.ChildAdded(this);
			}

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		public void MoveTo(IntPoint3D location)
		{
			var oldLocation = this.Location;

			this.Location = location;

			this.Parent.ChildMoved(this, oldLocation, location);

			if (ObjectMoved != null)
				ObjectMoved(this, this.Parent, this.Location);
		}

		public Environment Environment
		{
			get { return this.Parent as Environment; }
		}

		IEnvironment IGameObject.Environment
		{
			get { return this.Parent as IEnvironment; }
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (GameObjectData)_data;

			base.Deserialize(_data);

			GameObject env = null;
			if (data.Environment != ObjectID.NullObjectID)
				env = this.World.FindObject<GameObject>(data.Environment);

			MoveTo(env, data.Location);
		}

		public override string ToString()
		{
			return String.Format("Object({0})", this.ObjectID);
		}

		GameObject m_parent;
		public GameObject Parent
		{
			get { return m_parent; }
			private set { m_parent = value; Notify("Parent"); }
		}

		IGameObject IGameObject.Parent { get { return this.Parent; } }

		IntPoint3D m_location;
		public IntPoint3D Location
		{
			get { return m_location; }
			private set { m_location = value; Notify("Location"); }
		}
	}
}
