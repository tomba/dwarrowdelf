using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dwarrowdelf.Client
{
	public partial class App : Application
	{
		public App()
		{
			this.Startup += App_Startup;
		}

		async void App_Startup(object sender, StartupEventArgs e)
		{
			// This must be created in WPF context, so that SynchronizationContext is valid
			GameData.Create();

			var mainWindow = new MainWindowWpf();
			this.MainWindow = mainWindow;
			mainWindow.Show();

			if (ClientConfig.AutoConnect)
			{
				await StartServerAndConnectAsync();
			}
		}

		async Task StartServerAndConnectAsync()
		{
			//var dlg = App.GameWindow.OpenLogOnDialog();

			try
			{
				var prog = new Progress<string>(str => Trace.TraceInformation(str));

				var options = ClientConfig.EmbeddedServerOptions;

				await GameData.Data.ConnectManager.StartServerAndConnectAsync(options, ClientConfig.ConnectionType, prog);
			}
			catch (Exception exc)
			{
				MessageBox.Show(this.MainWindow, exc.ToString(), "Failed to autoconnect");
			}

			//dlg.Close();
		}
	}
}
