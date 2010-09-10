using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

using System.Windows.Resources;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Diagnostics;


namespace MyGame.Client
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public new static App Current { get { return (App)Application.Current; } }
		internal new static MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		Thread m_serverThread;
		EventWaitHandle m_serverStartWaitHandle;
		EventWaitHandle m_serverStopWaitHandle;
		RegisteredWaitHandle m_registeredWaitHandle;
		bool m_serverInAppDomain;
		IServer m_server;

		Window m_serverStartDialog; // Hacky dialog

		public IServer Server { get { return m_server; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			m_serverInAppDomain = true;

			Thread.CurrentThread.Name = "Main";

			base.OnStartup(e);

#if DEBUG
			bool debugClient = MyGame.Client.Properties.Settings.Default.DebugClient;

			if (debugClient)
			{
				var debugListener = new MMLogTraceListener("Client");
				Debug.Listeners.Clear();
				Debug.Listeners.Add(debugListener);
			}
#endif

			Debug.Print("Start");

			GameData.Data.Connection = new ClientConnection();

			if (m_serverInAppDomain)
			{
				m_serverStartWaitHandle = new AutoResetEvent(false);
				m_serverStopWaitHandle = new AutoResetEvent(false);

				m_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(m_serverStartWaitHandle,
					ServerStartedCallback, null, TimeSpan.FromMinutes(1), true);

				m_serverThread = new Thread(ServerThreadStart);
				m_serverThread.Start();

				m_serverStartDialog = new Window();
				m_serverStartDialog.Topmost = true;
				m_serverStartDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
				m_serverStartDialog.Width = 200;
				m_serverStartDialog.Height = 200;
				var label = new Label();
				label.Content = "Starting Server";
				m_serverStartDialog.Content = label;
				m_serverStartDialog.Show();
			}
		}

		void ServerStartedCallback(object state, bool timedOut)
		{
			if (timedOut)
				throw new Exception();

			m_registeredWaitHandle.Unregister(m_serverStartWaitHandle);
			m_registeredWaitHandle = null;
			m_serverStartWaitHandle.Close();
			m_serverStartWaitHandle = null;

			// XXX mainwindow is already open before server is up
			this.Dispatcher.BeginInvoke(new Action(ServerStartedCallback2), DispatcherPriority.Normal, null);
		}

		void ServerStartedCallback2()
		{
			m_serverStartDialog.Close();
			MainWindow.OnServerStarted();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (m_serverInAppDomain && m_serverStopWaitHandle != null)
			{
				m_serverStopWaitHandle.Set();
				m_serverThread.Join();
			}

			Debug.Print("Exiting");
		}


		void ServerThreadStart()
		{
			Thread.CurrentThread.Priority = ThreadPriority.Lowest;
			Thread.CurrentThread.Name = "Main";

			AppDomain domain = AppDomain.CreateDomain("ServerDomain");

			string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
			path = System.IO.Path.GetDirectoryName(path);
			path = System.IO.Path.Combine(path, "Server.exe");

			bool debugServer = MyGame.Client.Properties.Settings.Default.DebugServer;

			m_server = (IServer)domain.CreateInstanceFromAndUnwrap(path, "MyGame.Server.Server");
			m_server.RunServer(true, debugServer, m_serverStartWaitHandle, m_serverStopWaitHandle);

			AppDomain.Unload(domain);
		}
	}
}
