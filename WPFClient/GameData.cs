using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame
{
	class ActionCollection : ObservableCollection<GameAction> { }

	class GameData : INotifyPropertyChanged
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.ActionCollection = new ActionCollection();
			this.SymbolDrawings = new SymbolDrawings();
		}

		public Connection Connection { get; set; }

		PlayerObject m_player;
		public PlayerObject Player
		{
			get	{ return m_player; }

			set
			{
				m_player = value;
				Notify("Player");
			}
		}

		public ClientGameObject[] Objects
		{
			get { return ClientGameObject.GetObjects(); }
			set { Notify("Objects"); }
		}

		public bool DisableLOS { get; set; }	// debug
		public bool ShowChangedTiles { get; set; } // debug

		public MyTraceListener MyTraceListener { get; set; }

		public ActionCollection ActionCollection { get; private set; }

		public SymbolDrawings SymbolDrawings { get; private set; }


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

		void Notify(String info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

	}
}
