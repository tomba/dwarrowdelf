using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Xml.Serialization;
using System.Windows.Resources;
using System.Threading;
using System.Diagnostics;


namespace MyGame
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		Thread m_serverThread;
		Connection m_connection;
		EventWaitHandle m_serverWaitHandle;
		bool m_serverInAppDomain;

		protected override void OnStartup(StartupEventArgs e)
		{
			MyDebug.Prefix = "[Client] ";

			base.OnStartup(e);

			GameData.Data.MyTraceListener = new MyTraceListener();
			Debug.Listeners.Add(GameData.Data.MyTraceListener);

			m_serverInAppDomain = false;

			if (m_serverInAppDomain)
			{
				m_serverWaitHandle =
					new EventWaitHandle(false, EventResetMode.AutoReset, "MyGame.ServerWaitHandle");
				StartServer();
			}

			m_connection = new Connection();
			m_connection.Connect();
			m_connection.Server.Login("tomba");
			GameData.Data.Connection = m_connection;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			if(!m_connection.HasFaulted())
				m_connection.Server.Logout();
			m_connection.Disconnect();

			if (m_serverInAppDomain)
			{
				m_serverWaitHandle.Set();
				m_serverThread.Join();
			}

			MyDebug.WriteLine("Exiting");
		}

		void StartServer()
		{
			m_serverThread = new Thread(ServerThreadStart);
			m_serverThread.Start();

			MyDebug.WriteLine("Client waiting for server start");
			m_serverWaitHandle.WaitOne();
			MyDebug.WriteLine("Client got server start");
		}

		void ServerThreadStart()
		{
			AppDomain domain = AppDomain.CreateDomain("ServerDomain");
			domain.SetData("DebugTextWriter", GameData.Data.MyTraceListener);

			string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
			path = System.IO.Path.GetDirectoryName(path);
			path = System.IO.Path.Combine(path, "ServerBase.dll");

			IServer server = (IServer)domain.CreateInstanceFromAndUnwrap(path, "MyGame.Server");
			server.RunServer(true);

			AppDomain.Unload(domain);
		}
	}
}
