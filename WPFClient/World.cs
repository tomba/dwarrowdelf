using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MyGame
{
	class World : INotifyPropertyChanged
	{
		public static World TheWorld { get; set; }

		public IAreaData AreaData { get; private set; }
		public ObservableCollection<Environment> Environments { get; private set; }

		public ObjectCollection Controllables { get; private set; }

		ObservableObjectCollection m_objects;
		public ReadOnlyObservableObjectCollection Objects { get; private set; }

		public SymbolDrawingCache SymbolDrawings { get; private set; }

		// perhaps this is not needed in client side
		public int UserID { get; set; }

		public World(IAreaData areaData)
		{
			this.AreaData = areaData;
			this.Environments = new ObservableCollection<Environment>();
			this.Controllables = new ObjectCollection();
			m_objects = new ObservableObjectCollection();
			this.Objects = new ReadOnlyObservableObjectCollection(m_objects);
			this.SymbolDrawings = new SymbolDrawingCache(areaData);
		}

		public void AddEnvironment(Environment env)
		{
			this.Environments.Add(env);
		}

		int m_turnNumber;
		public int TurnNumber
		{
			get { return m_turnNumber; }
			set { m_turnNumber = value; Notify("TurnNumber"); }
		}



		internal void AddObject(ClientGameObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException();

			m_objects.Add(ob);
		}

		public ClientGameObject FindObject(ObjectID objectID)
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
