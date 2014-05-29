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
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
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

		public async Task StartServerAndConnectAsync()
		{
			var dlg = this.MainWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));

				var path = Win32.SavedGamesFolder.GetSavedGamesPath();
				path = System.IO.Path.Combine(path, "Dwarrowdelf", "save");

				var options = new EmbeddedServerOptions()
				{
					ServerMode = ClientConfig.EmbeddedServerMode,
					NewGameOptions = ClientConfig.NewGameOptions,
					SaveGamePath = path,
					CleanSaveDir = ClientConfig.CleanSaveDir,
				};

				await this.ConnectManager.StartServerAndConnectAsync(options, ClientConfig.ConnectionType, prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(this.MainWindow, exc.ToString(), "Failed to autoconnect");
			}

			dlg.Close();
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

			user.DisconnectEvent += user_DisconnectEvent;

			m_turnHandler = new TurnHandler(this.World, this.User);

			var controllable = this.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				var mapControl = App.GameWindow.MapControl;
				mapControl.IsVisibilityCheckEnabled = !user.IsSeeAll;
				mapControl.GoTo(controllable.Environment, controllable.Location);

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
			App.GameWindow.MapControl.GoTo(null, new IntPoint3());
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
