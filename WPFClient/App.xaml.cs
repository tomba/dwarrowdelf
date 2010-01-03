using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Windows.Resources;
using System.Threading;


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

		public IServer Server { get { return m_server; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			m_serverInAppDomain = true;

			MyDebug.DefaultFlags = DebugFlag.Client;

			base.OnStartup(e);

#if DEBUG
			bool debugClient = MyGame.Client.Properties.Settings.Default.DebugClient;
			bool debugServer = m_serverInAppDomain && MyGame.Client.Properties.Settings.Default.DebugServer;

			if (debugClient || debugServer)
			{
			}
#endif

			MyDebug.WriteLine("Start");

			GameData.Data.Connection = new ClientConnection();

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
				//m_connection.BeginConnect(ConnectCallback);
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

			//m_connection.BeginConnect(ConnectCallback);
		}

		void ConnectCallback()
		{
			//m_connection.Send(new ClientMsgs.LogOnRequest() { Name = "tomba" });
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			var conn = GameData.Data.Connection;

			if (conn.IsCharConnected)
			{
				conn.Send(new ClientMsgs.LogOffCharRequest());
			}

			if (conn.IsUserConnected)
			{
				conn.Send(new ClientMsgs.LogOffRequest());
				Thread.Sleep(100);	// XXX wait for LogOffReply.
				conn.Disconnect();
				GameData.Data.Connection = null;
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

			m_server = (IServer)domain.CreateInstanceFromAndUnwrap(path, "MyGame.Server.Server");
			m_server.RunServer(true, m_serverStartWaitHandle, m_serverStopWaitHandle);

			AppDomain.Unload(domain);
		}
	}
}
