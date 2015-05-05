using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Text;
using Dwarrowdelf.Client.UI;
using System.Threading.Tasks;

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
			if (e.Args.Contains("adventure"))
				ClientConfig.EmbeddedServerOptions.NewGameOptions.Mode = GameMode.Adventure;
			else if (e.Args.Contains("fortress"))
				ClientConfig.EmbeddedServerOptions.NewGameOptions.Mode = GameMode.Fortress;

			// this will force instantiating GameData.Data in the main thread
			GameData.Create();

			GameData.Data.UserConnected += Data_UserConnected;
			GameData.Data.UserDisconnected += Data_UserDisconnected;

			var mainWindow = new UI.MainWindow();
			this.MainWindow = mainWindow;
			mainWindow.Show();

			if (ClientConfig.AutoConnect)
			{
				await StartServerAndConnectAsync();
			}
		}

		void Data_UserDisconnected()
		{
			App.GameWindow.MapControl.GoTo(null);
		}

		void Data_UserConnected()
		{
			var data = GameData.Data;

			if (data.GameMode == GameMode.Adventure)
				App.GameWindow.MapControl.GoTo(data.FocusedObject);
			else
				App.GameWindow.MapControl.GoTo(data.World.Controllables.First());

			if (Program.StartupStopwatch != null)
			{
				Program.StartupStopwatch.Stop();
				Trace.WriteLine(String.Format("Startup {0} ms", Program.StartupStopwatch.ElapsedMilliseconds));
				Program.StartupStopwatch = null;
			}
		}

		async Task StartServerAndConnectAsync()
		{
			var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => dlg.AppendText(str));

				var options = ClientConfig.EmbeddedServerOptions;

				await GameData.Data.ConnectManager.StartServerAndConnectAsync(options, ClientConfig.ConnectionType, prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(this.MainWindow, exc.ToString(), "Failed to autoconnect");
			}

			dlg.Close();
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



		static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exc = (Exception)e.ExceptionObject;

			ExceptionHelper.DumpException(exc, "AppDomain Unhandled Exception");

			if (e.IsTerminating == false)
				Environment.Exit(0);
		}
	}
}
