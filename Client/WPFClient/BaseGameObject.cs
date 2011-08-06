using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Dwarrowdelf.Client
{
	abstract class BaseGameObject : IBaseGameObject, INotifyPropertyChanged
	{
		public ObjectID ObjectID { get; private set; }
		public ObjectType ObjectType { get { return this.ObjectID.ObjectType; } }
		public World World { get; private set; }
		IWorld IBaseGameObject.World { get { return this.World as IWorld; } }
		public bool IsDestructed { get; private set; }

		public event Action<BaseGameObject> Destructed;

		event Action<IBaseGameObject> IBaseGameObject.Destructed
		{
			add { lock (this.Destructed) this.Destructed += value; }
			remove { lock (this.Destructed) this.Destructed -= value; }
		}

		protected BaseGameObject(World world, ObjectID objectID)
		{
			this.ObjectID = objectID;
			this.World = world;
			this.World.AddObject(this);
		}

		public virtual void Destruct()
		{
			if (this.IsDestructed)
				throw new Exception();

			this.IsDestructed = true;

			if (this.Destructed != null)
				this.Destructed(this);

			this.World.RemoveObject(this);
		}

		public virtual void Deserialize(BaseGameObjectData data)
		{
			foreach (var tuple in data.Properties)
				SetProperty(tuple.Item1, tuple.Item2);
		}

		public virtual object Save() { return null; }
		public virtual void Restore(object data) { }

		public abstract void SetProperty(PropertyID propertyID, object value);

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}
}
