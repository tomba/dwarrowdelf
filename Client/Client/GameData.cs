using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	public enum GameSpeed
	{
		Immediate,
		Fastest,
		Fast,
		Normal,
		Slow,
	}

	sealed class GameData : INotifyPropertyChanged
	{
		public static readonly GameData Data = new GameData();

		GameData()
		{
			this.TileSet = new TileSet(new Uri("/Dwarrowdelf.Client;component/TileSet/TileSet.png", UriKind.Relative));

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate
			{
				if (_Blink != null) _Blink();
				if (MainWindow != null && MainWindow.MapControl != null)
					MainWindow.MapControl.Blink();
			};

			this.ConnectManager = new ConnectManager();
			this.ConnectManager.UserConnected += ConnectManager_UserConnected;
		}

		public ConnectManager ConnectManager { get; private set; }

		public ClientNetStatistics NetStats { get { return this.ConnectManager.NetStats; } }

		TurnHandler m_turnHandler;

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

		public event Action<GameMode> GameModeChanged;

		GameMode m_gameMode;
		public GameMode GameMode
		{
			get { return m_gameMode; }
			set { m_gameMode = value; Notify("GameMode"); if (this.GameModeChanged != null) this.GameModeChanged(value); }
		}

		public event Action<bool> IsVisibilityCheckEnabledChanged;

		bool m_isVisibilityCheckEnabled;
		public bool IsVisibilityCheckEnabled
		{
			get { return m_isVisibilityCheckEnabled; }
			set { m_isVisibilityCheckEnabled = value; Notify("IsVisibilityCheckEnabled"); if (this.IsVisibilityCheckEnabledChanged != null) this.IsVisibilityCheckEnabledChanged(value); }
		}

		bool m_autoAdvanceTurnEnabled;
		public bool IsAutoAdvanceTurn
		{
			get { return m_autoAdvanceTurnEnabled; }
			set
			{
				m_autoAdvanceTurnEnabled = value;

				if (m_turnHandler != null)
				{
					m_turnHandler.IsAutoAdvanceTurnEnabled = value;

					if (value == true && (this.FocusedObject == null || this.FocusedObject.HasAction))
						m_turnHandler.SendProceedTurn();
				}

				Notify("IsAutoAdvanceTurn");
			}
		}

		public void SendProceedTurn()
		{
			if (m_turnHandler != null)
				m_turnHandler.SendProceedTurn();
		}

		void ConnectManager_UserConnected(ClientUser user)
		{
			if (this.User != null)
				throw new Exception();

			this.User = user;
			this.GameMode = user.GameMode;
			this.World = user.World;
			this.IsVisibilityCheckEnabled = !user.IsSeeAll;

			user.DisconnectEvent += user_DisconnectEvent;

			m_turnHandler = new TurnHandler(this.World, this.User);

			var controllable = this.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				App.GameWindow.MapControl.GoTo(controllable);

				if (this.GameMode == GameMode.Adventure)
					this.FocusedObject = controllable;
			}

			if (Program.StartupStopwatch != null)
			{
				Program.StartupStopwatch.Stop();
				Trace.WriteLine(String.Format("Startup {0} ms", Program.StartupStopwatch.ElapsedMilliseconds));
				Program.StartupStopwatch = null;
			}
		}

		void user_DisconnectEvent()
		{
			this.User.DisconnectEvent -= user_DisconnectEvent;

			this.FocusedObject = null;
			App.GameWindow.MapControl.GoTo(null);
			this.User = null;
			this.World = null;
			m_turnHandler = null;
		}


		public event Action<LivingObject, LivingObject> FocusedObjectChanged;

		LivingObject m_focusedObject;
		public LivingObject FocusedObject
		{
			get { return m_focusedObject; }

			set
			{
				if (m_focusedObject == value)
					return;

				var old = m_focusedObject;
				m_focusedObject = value;

				if (m_turnHandler != null)
					m_turnHandler.FocusedObject = value;

				if (this.FocusedObjectChanged != null)
					this.FocusedObjectChanged(old, value);

				Notify("FocusedObject");
			}
		}

		public event Action<GameSpeed> GameSpeedChanged;

		// XXX we should get this from the server
		GameSpeed m_gameSpeed = GameSpeed.Fast;
		public GameSpeed GameSpeed
		{
			get { return m_gameSpeed; }
			set
			{
				m_gameSpeed = value;
				Notify("GameSpeed");
				if (this.GameSpeedChanged != null)
					this.GameSpeedChanged(value);

				// XXX this should probably be somewhere else
				if (this.User != null)
				{
					int ms;

					switch (value)
					{
						case Client.GameSpeed.Immediate:
							ms = 0;
							break;
						case Client.GameSpeed.Fastest:
							ms = 1;
							break;
						case Client.GameSpeed.Fast:
							ms = 50;
							break;
						case Client.GameSpeed.Normal:
							ms = 10;
							break;
						case Client.GameSpeed.Slow:
							ms = 250;
							break;
						default:
							throw new Exception();
					}

					this.User.Send(new Dwarrowdelf.Messages.SetWorldConfigMessage()
					{
						MinTickTime = TimeSpan.FromMilliseconds(ms),
					});
				}
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
