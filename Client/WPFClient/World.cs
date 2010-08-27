using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace MyGame.Client
{
	class World : INotifyPropertyChanged
	{
		IdentifiableCollection m_objects;
		public ReadOnlyIdentifiableCollection Objects { get; private set; }

		EnvironmentCollection m_environments;
		public ReadOnlyEnvironmentCollection Environments { get; private set; }
		
		public LivingCollection Controllables { get; private set; }

		public SymbolDrawingCache SymbolDrawingCache { get; private set; }

		public MyGame.Jobs.JobManager JobManager { get; private set; }

		// perhaps this is not needed in client side
		public int UserID { get; set; }

		public World()
		{
			m_objects = new IdentifiableCollection();
			this.Objects = new ReadOnlyIdentifiableCollection(m_objects);

			m_environments = new EnvironmentCollection();
			this.Environments = new ReadOnlyEnvironmentCollection(m_environments);

			this.Controllables = new LivingCollection();

			this.SymbolDrawingCache = new SymbolDrawingCache(new Uri("SymbolInfosChar.xaml", UriKind.Relative));

			this.JobManager = new MyGame.Jobs.JobManager();
			this.TickIncreased += this.JobManager.DoHouseKeeping;
		}

		internal void AddEnvironment(Environment env)
		{
			m_environments.Add(env);
		}

		int m_tickNumber;
		public int TickNumber
		{
			get { return m_tickNumber; }
			set
			{
				// XXX perhaps this should be IncreaseTick()
				m_tickNumber = value; Notify("TickNumber");

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

		public T FindObject<T>(ObjectID objectID) where T : class, IIdentifiable
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
