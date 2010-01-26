using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MyGame.Client
{
	class World : INotifyPropertyChanged
	{
		public static World TheWorld { get; set; }

		public IAreaData AreaData { get; private set; }
		public ObservableCollection<Environment> Environments { get; private set; }

		public LivingCollection Controllables { get; private set; }

		KeyedObjectCollection m_objects;
		public ReadOnlyKeyedObjectCollection Objects { get; private set; }

		public ObservableCollection<IJob> Jobs { get; private set; }

		public DrawingCache DrawingCache { get; private set; }
		public SymbolDrawingCache SymbolDrawingCache { get; private set; }

		// perhaps this is not needed in client side
		public int UserID { get; set; }

		public World(IAreaData areaData)
		{
			this.AreaData = areaData;
			this.Environments = new ObservableCollection<Environment>();
			this.Controllables = new LivingCollection();
			m_objects = new KeyedObjectCollection();
			this.Objects = new ReadOnlyKeyedObjectCollection(m_objects);

			this.DrawingCache = new DrawingCache(areaData);
			this.SymbolDrawingCache = new SymbolDrawingCache(this.DrawingCache, areaData.Symbols);

			this.Jobs = new ObservableCollection<IJob>();
		}

		public void AddEnvironment(Environment env)
		{
			this.Environments.Add(env);
		}

		int m_tickNumber;
		public int TickNumber
		{
			get { return m_tickNumber; }
			set
			{
				// XXX perhaps this should be IncreaseTick()
				m_tickNumber = value; Notify("TickNumber");

				var doneJobs = this.Jobs.Where(j => j.Progress == Progress.Done).ToArray();
				foreach (var job in doneJobs)
					this.Jobs.Remove(job);

				if (TickIncreased != null)
					TickIncreased();
			}
		}

		public event Action TickIncreased;

		internal void AddObject(IIdentifiable ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			m_objects.Add(ob);
		}

		internal void RemoveObject(IIdentifiable ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			if (m_objects.Remove(ob) == false)
				throw new Exception();
		}

		public IIdentifiable FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			if (m_objects.Contains(objectID))
				return m_objects[objectID];
			else
				return null;
		}

		public T FindObject<T>(ObjectID objectID) where T : ClientGameObject
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}
