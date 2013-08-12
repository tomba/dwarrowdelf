using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	sealed class GameData : INotifyPropertyChanged
	{
		public static GameData Data;

		public GameData()
		{
			this.TileSet = new TileSet(new Uri("/Dwarrowdelf.Client;component/TileSet/TileSet.png", UriKind.Relative));

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate { if (_Blink != null) _Blink(); MainWindow.MapControl.InvalidateTileData(); };

			this.ConnectManager = new ConnectManager();
		}

		public ConnectManager ConnectManager { get; private set; }

		public ClientNetStatistics NetStats { get { return this.ConnectManager.NetStats; } }

		DispatcherTimer m_timer;

		event Action _Blink;

		public event Action Blink
		{
			add
			{
				//if (DesignerProperties.GetIsInDesignMode(this))
				//	return;

				if (_Blink == null)
					m_timer.IsEnabled = true;

				_Blink = (Action)Delegate.Combine(_Blink, value);
			}

			remove
			{
				//if (DesignerProperties.GetIsInDesignMode(this))
				//	return;

				_Blink = (Action)Delegate.Remove(_Blink, value);

				if (_Blink == null)
					m_timer.IsEnabled = false;
			}
		}

		public UI.MainWindow MainWindow { get { return (UI.MainWindow)Application.Current.MainWindow; } }

		public event Action TileSetChanged;

		TileSet m_tileSet;
		public TileSet TileSet
		{
			get { return m_tileSet; }
			set { m_tileSet = value; if (this.TileSetChanged != null) this.TileSetChanged(); }
		}

		ClientUser m_user;
		public ClientUser User
		{
			get { return m_user; }
			set { m_user = value; Notify("User"); }
		}

		public event Action<World> WorldChanged;

		World m_world;
		public World World
		{
			get { return m_world; }
			set { m_world = value; Notify("World"); if (this.WorldChanged != null) this.WorldChanged(value); }
		}

		bool m_autoAdvanceTurnEnabled;
		public bool IsAutoAdvanceTurn
		{
			get { return m_autoAdvanceTurnEnabled; }
			set
			{
				m_autoAdvanceTurnEnabled = value;

				if (m_user != null && value == true)
				{
					if (App.GameWindow.FocusedObject == null || App.GameWindow.FocusedObject.HasAction)
						m_user.SendProceedTurn();
				}

				Notify("IsAutoAdvanceTurn");
			}
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
