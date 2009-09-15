using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame
{
	public static class MyExtensions
	{
		public static System.Windows.Media.Color ToColor(this GameColor color)
		{
			return System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
		}
	}   

	class ActionCollection : ObservableCollection<GameAction> { }
	class ObjectCollection : ObservableCollection<ClientGameObject> { }

	class GameData : INotifyPropertyChanged
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.ActionCollection = new ActionCollection();
			this.Controllables = new ObjectCollection();
			this.SymbolDrawings = new SymbolDrawingCache(World.TheWorld.AreaData);

			this.Objects = new ObjectCollection();
		}

		public int UserID { get; set; }
		public Connection Connection { get; set; }

		public ObjectCollection Controllables { get; private set; }

		ClientGameObject m_currentObject;
		public ClientGameObject CurrentObject
		{
			get { return m_currentObject; }
			set { m_currentObject = value; Notify("CurrentObject"); }
		}

		public bool DisableLOS { get; set; }	// debug

		public MyTraceListener MyTraceListener { get; set; }

		public ActionCollection ActionCollection { get; private set; }

		public SymbolDrawingCache SymbolDrawings { get; private set; }

		public ObjectCollection Objects { get; private set; }
		public ObservableCollection<Environment> Environments { get { return World.TheWorld.Environments; } }

		int m_turnNumber;
		public int TurnNumber
		{
			get { return m_turnNumber; }

			set
			{
				m_turnNumber = value;
				Notify("TurnNumber");
			}
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
