using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef(ClientObject = true)]
	abstract class BaseObject : IBaseObject, INotifyPropertyChanged
	{
		public ObjectID ObjectID { get; private set; }
		public ObjectType ObjectType { get { return this.ObjectID.ObjectType; } }

		public DateTime CreationTime { get; private set; }
		public int CreationTick { get; private set; }
		public DateTime ClientCreationTime { get; private set; }

		public World World { get; private set; }
		IWorld IBaseObject.World { get { return this.World as IWorld; } }
		public bool IsDestructed { get; private set; }

		public event Action<BaseObject> Destructed;

		public bool IsInitialized { get; private set; }

		event Action<IBaseObject> IBaseObject.Destructed
		{
			add { lock (this.Destructed) this.Destructed += value; }
			remove { lock (this.Destructed) this.Destructed -= value; }
		}

		protected BaseObject(World world, ObjectID objectID)
		{
			this.ObjectID = objectID;
			this.World = world;
			this.ClientCreationTime = DateTime.Now;
		}

		public virtual void Destruct()
		{
			if (this.IsDestructed)
				throw new Exception();

			this.IsDestructed = true;
			Notify("IsDestructed");

			if (this.Destructed != null)
				this.Destructed(this);
		}

		public virtual void ReceiveObjectData(BaseGameObjectData data)
		{
			this.CreationTime = data.CreationTime;
			this.CreationTick = data.CreationTick;

			if (data.Properties != null)
			{
				foreach (var tuple in data.Properties)
					SetProperty(tuple.Item1, tuple.Item2);
			}

			this.IsInitialized = true;
			Notify("IsInitialized");
		}

		public virtual void ReceiveObjectDataEnd()
		{
		}

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
