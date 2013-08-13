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
		public static GameData Data { get; private set; }

		public static void InitGameData()
		{
			if (Data != null)
				throw new Exception();

			Data = new GameData();
		}

		GameData()
		{
			this.TileSet = new TileSet(new Uri("/Dwarrowdelf.Client;component/TileSet/TileSet.png", UriKind.Relative));

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate { if (_Blink != null) _Blink(); MainWindow.MapControl.InvalidateTileData(); };

			this.ConnectManager = new ConnectManager();
			this.ConnectManager.UserConnected += ConnectManager_UserConnected;
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
					if (this.FocusedObject == null || this.FocusedObject.HasAction)
						m_user.SendProceedTurn();
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
					NewGameMode = ClientConfig.NewGameMode,
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

		void ConnectManager_UserConnected(ClientUser user)
		{
			if (this.User != null)
				throw new Exception();

			this.User = user;
			this.World = user.World;

			user.DisconnectEvent += user_DisconnectEvent;

			var controllable = this.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				var mapControl = App.GameWindow.MapControl;
				mapControl.IsVisibilityCheckEnabled = !user.IsSeeAll;
				mapControl.Environment = controllable.Environment;
				mapControl.CenterPos = new Point(controllable.Location.X, controllable.Location.Y);
				mapControl.Z = controllable.Location.Z;

				if (this.World.GameMode == GameMode.Adventure)
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
			App.GameWindow.MapControl.Environment = null;
			this.User = null;
			this.World = null;
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
