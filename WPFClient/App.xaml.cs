using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Xml.Serialization;
using System.Windows.Resources;
using System.Threading;


namespace MyGame
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static DebugWindow DebugWindow { get; set; }
		public new static App Current { get { return (App)Application.Current; } }
		internal new static MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		Thread m_serverThread;
		ClientConnection m_connection;
		EventWaitHandle m_serverStartWaitHandle;
		EventWaitHandle m_serverStopWaitHandle;
		RegisteredWaitHandle m_registeredWaitHandle;
		bool m_serverInAppDomain;
		IServer m_server;

		public IServer Server { get { return m_server; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			m_serverInAppDomain = true;

			MyDebug.Prefix = "[Client] ";

			base.OnStartup(e);

#if DEBUG
			bool debugClient = MyGame.Properties.Settings.Default.DebugClient;
			bool debugServer = m_serverInAppDomain && MyGame.Properties.Settings.Default.DebugServer;

			if (debugClient || debugServer)
			{
				DebugWindow = new DebugWindow();
				DebugWindow.Show();
				DebugWindow.WindowState = WindowState.Maximized;
			}
#endif

			MyDebug.WriteLine("Start");

			var areaData = new MyAreaData.AreaData();
			var world = new World(areaData);
			World.TheWorld = world;


			m_connection = new ClientConnection();

			if (m_serverInAppDomain)
			{
				m_serverStartWaitHandle = new AutoResetEvent(false);
				m_serverStopWaitHandle = new AutoResetEvent(false);

				m_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(m_serverStartWaitHandle,
					ServerStartedCallback, null, TimeSpan.FromMinutes(1), true);

				m_serverThread = new Thread(ServerThreadStart);
				m_serverThread.Start();
			}
			else
			{
				m_connection.BeginConnect(ConnectCallback);
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

			m_connection.BeginConnect(ConnectCallback);
		}

		void ConnectCallback()
		{
			GameData.Data.Connection = m_connection;
			m_connection.Send(new ClientMsgs.LogOnRequest() { Name = "tomba" });
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if (m_connection != null)
			{
				m_connection.Send(new ClientMsgs.LogOffMessage());
				m_connection.Disconnect();
				m_connection = null;
			}

			if (m_serverInAppDomain && m_serverStopWaitHandle != null)
			{
				m_serverStopWaitHandle.Set();
				m_serverThread.Join();
			}

			MyDebug.WriteLine("Exiting");
		}


		void ServerThreadStart()
		{
			AppDomain domain = AppDomain.CreateDomain("ServerDomain");

			string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
			path = System.IO.Path.GetDirectoryName(path);
			path = System.IO.Path.Combine(path, "Server.exe");

			m_server = (IServer)domain.CreateInstanceFromAndUnwrap(path, "MyGame.Server");
			m_server.TraceListener = GameData.Data.MyTraceListener;
			m_server.RunServer(true, m_serverStartWaitHandle, m_serverStopWaitHandle);

			AppDomain.Unload(domain);
		}
	}
}
