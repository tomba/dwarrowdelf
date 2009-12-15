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

	class GameData : INotifyPropertyChanged
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.ActionCollection = new ActionCollection();
		}


		ClientConnection m_connection;
		public ClientConnection Connection
		{
			get { return m_connection; }
			set { m_connection = value; Notify("Connection"); }
		}

		Living m_currentObject;
		public Living CurrentObject
		{
			get { return m_currentObject; }
			set { m_currentObject = value; Notify("CurrentObject"); }
		}

		World m_world;
		public World World
		{
			get { return m_world; }
			set { m_world = value; Notify("World"); }
		}

		public bool IsSeeAll { get; set; }
		public bool DisableLOS { get; set; }	// debug

		public MyDebugListener MyTraceListener { get; set; }

		public ActionCollection ActionCollection { get; private set; }


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
