using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MyGame
{
	class GameData : INotifyPropertyChanged
	{
		public static readonly GameData Data = new GameData();

		public Connection Connection { get; set; }

		public GameObject Player { get; set; }

		public MyTraceListener MyTraceListener { get; set; }

		int m_turnNumber;
		public int TurnNumber
		{
			get
			{
				return m_turnNumber;
			}

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
