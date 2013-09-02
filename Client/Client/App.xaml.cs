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
			if (e.Args.Contains("adventure"))
				ClientConfig.NewGameOptions.Mode = GameMode.Adventure;
			else if (e.Args.Contains("fortress"))
				ClientConfig.NewGameOptions.Mode = GameMode.Fortress;

			GameData.InitGameData();
			
			var mainWindow = new UI.MainWindow();
			this.MainWindow = mainWindow;
			mainWindow.Show();

			if (ClientConfig.AutoConnect)
			{
				await GameData.Data.StartServerAndConnectAsync();
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
