using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Text;
using Dwarrowdelf.Client.UI;

namespace Dwarrowdelf.Client
{
	sealed partial class App : Application
	{
		internal static UI.MainWindow GameWindow { get { return (UI.MainWindow)Application.Current.MainWindow; } }

		public App()
		{
			if (Debugger.IsAttached == false)
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			this.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;

			this.Startup += App_Startup;
			this.Activated += App_Activated;
			this.Deactivated += App_Deactivated;
			this.Exit += App_Exit;
		}

		async void App_Startup(object sender, StartupEventArgs e)
		{
			GameData.Data = new GameData();

			GameData.Data.ConnectManager.UserConnected += ConnectManager_UserConnected;

			if (e.Args.Contains("adventure"))
				ClientConfig.NewGameMode = GameMode.Adventure;
			else if (e.Args.Contains("fortress"))
				ClientConfig.NewGameMode = GameMode.Fortress;

			var mainWindow = new UI.MainWindow();
			this.MainWindow = mainWindow;
			mainWindow.Show();

			if (ClientConfig.AutoConnect)
			{
				var dlg = mainWindow.OpenLogOnDialog();

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

					await GameData.Data.ConnectManager.StartServerAndConnectAsync(options, ClientConfig.ConnectionType, prog);
				}
				catch (Exception exc)
				{
					MessageBox.Show(this.MainWindow, exc.ToString(), "Failed to autoconnect");
				}

				dlg.Close();
			}
		}

		void App_Exit(object sender, ExitEventArgs e)
		{
		}

		void App_Activated(object sender, EventArgs e)
		{
			// pause server?
		}

		void App_Deactivated(object sender, EventArgs e)
		{
			// continue server?
		}


		void ConnectManager_UserConnected(ClientUser user)
		{
			if (GameData.Data.User != null)
				throw new Exception();

			GameData.Data.User = user;
			GameData.Data.World = user.World;

			user.DisconnectEvent += user_DisconnectEvent;

			var controllable = GameData.Data.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				var mapControl = App.GameWindow.MapControl;
				mapControl.IsVisibilityCheckEnabled = !user.IsSeeAll;
				mapControl.Environment = controllable.Environment;
				mapControl.CenterPos = new Point(controllable.Location.X, controllable.Location.Y);
				mapControl.Z = controllable.Location.Z;

				if (GameData.Data.World.GameMode == GameMode.Adventure)
					App.GameWindow.FocusedObject = controllable;
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
			App.GameWindow.FocusedObject = null;
			App.GameWindow.MapControl.Environment = null;
			GameData.Data.User = null;
			GameData.Data.World = null;
		}




		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exc = (Exception)e.ExceptionObject;

			DumpException(exc);

			if (e.IsTerminating == false)
				Environment.Exit(0);
		}

		void DumpException(Exception e)
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				"dwarrowdelf-crash.txt");

			StringBuilder sb = new StringBuilder();

			sb.AppendLine(DateTime.Now.ToString());

			sb.AppendLine("---");

			sb.Append(e.ToString());

			File.WriteAllText(path, sb.ToString());
		}
	}
}
