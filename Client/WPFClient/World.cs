using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Dwarrowdelf.Client
{
	class World : IWorld, INotifyPropertyChanged
	{
		BaseGameObjectCollection m_objects;
		public ReadOnlyBaseGameObjectCollection Objects { get; private set; }

		EnvironmentCollection m_environments;
		public ReadOnlyEnvironmentCollection Environments { get; private set; }
		
		public LivingCollection Controllables { get; private set; }

		public SymbolDrawingCache SymbolDrawingCache { get; private set; }

		public Dwarrowdelf.Jobs.JobManager JobManager { get; private set; }

		// perhaps this is not needed in client side
		public int UserID { get; set; }

		public World()
		{
			m_objects = new BaseGameObjectCollection();
			this.Objects = new ReadOnlyBaseGameObjectCollection(m_objects);

			m_environments = new EnvironmentCollection();
			this.Environments = new ReadOnlyEnvironmentCollection(m_environments);

			this.Controllables = new LivingCollection();

			this.SymbolDrawingCache = new SymbolDrawingCache(new Uri("/Symbols/SymbolInfosChar.xaml", UriKind.Relative));

			this.JobManager = new Dwarrowdelf.Jobs.JobManager(this);
		}

		internal void AddEnvironment(Environment env)
		{
			m_environments.Add(env);
		}

		int m_tickNumber;
		public int TickNumber
		{
			get { return m_tickNumber; }
			private set { m_tickNumber = value; Notify("TickNumber"); }
		}

		Random m_random = new Random();
		public Random Random { get { return m_random; } }

		public void SetTick(int tick)
		{
			this.TickNumber = tick;
		}

		public void HandleChange(TickStartChange change)
		{
			this.TickNumber = change.TickNumber;

			if (TickStarting != null)
				TickStarting();
		}

		public event Action TickStarting;

		internal void AddObject(IBaseGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			m_objects.Add(ob);
		}

		internal void RemoveObject(IBaseGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			if (m_objects.Remove(ob) == false)
				throw new Exception();
		}

		public IBaseGameObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			if (m_objects.Contains(objectID))
				return m_objects[objectID];
			else
				return null;
		}

		public T FindObject<T>(ObjectID objectID) where T : class, IBaseGameObject
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		public IBaseGameObject GetObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			if (m_objects.Contains(objectID))
				return m_objects[objectID];

			switch (objectID.ObjectType)
			{
				case ObjectType.Environment:
					return new Environment(this, objectID);

				case ObjectType.Living:
					return new Living(this, objectID);

				case ObjectType.Item:
					return new ItemObject(this, objectID);

				case ObjectType.Building:
					return new BuildingObject(this, objectID);

				default:
					throw new Exception();
			}
		}

		public T GetObject<T>(ObjectID objectID) where T : class, IBaseGameObject
		{
			return (T)GetObject(objectID);
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
