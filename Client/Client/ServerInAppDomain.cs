using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf.Client
{
	class ServerInAppDomain
	{
		EventWaitHandle m_serverStartWaitHandle;
		RegisteredWaitHandle m_registeredWaitHandle;

		SaveManager m_saveManager;

		EmbeddedServer m_embeddedServer;

		public event Action Started;
		public event Action<string> StatusChanged;

		public void Start()
		{
			var gameDir = @"C:\Users\Tomba\Work\Dwarrowdelf\save";

			bool cleanSaves = true;
			Guid save = Guid.Empty;

			m_saveManager = new SaveManager(gameDir);

			if (cleanSaves)
				m_saveManager.DeleteAll();
			else
				save = m_saveManager.GetLatestSaveFile();

			m_serverStartWaitHandle = new AutoResetEvent(false);

			m_registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(m_serverStartWaitHandle,
				ServerStartedCallback, null, TimeSpan.FromMinutes(1), true);

			m_embeddedServer = new EmbeddedServer(gameDir, save);
			m_embeddedServer.ServerStatusChangeEvent += OnServerStatusChange;
			m_embeddedServer.Start(m_serverStartWaitHandle);
		}

		public void Stop()
		{
			m_embeddedServer.Stop();
		}

		void OnServerStatusChange(string status)
		{
			if (this.StatusChanged != null)
				this.StatusChanged(status);
		}

		void ServerStartedCallback(object state, bool timedOut)
		{
			if (timedOut)
				throw new Exception();

			m_registeredWaitHandle.Unregister(m_serverStartWaitHandle);
			m_registeredWaitHandle = null;
			m_serverStartWaitHandle.Close();
			m_serverStartWaitHandle = null;

			if (this.Started != null)
				this.Started();
		}

		class EmbeddedServer
		{
			IGame m_game;
			Thread m_serverThread;
			EventWaitHandle m_serverStartWaitHandle;
			AppDomain m_serverDomain;
			Guid m_save;

			public Action<string> ServerStatusChangeEvent;

			public EmbeddedServer(string gameDir, Guid save)
			{
				m_save = save;

				var di = AppDomain.CurrentDomain.SetupInformation;

				var domainSetup = new AppDomainSetup()
				{
					ApplicationBase = di.ApplicationBase,
					ConfigurationFile = di.ApplicationBase + "Dwarrowdelf.Server.exe.config",
				};

				m_serverDomain = AppDomain.CreateDomain("ServerDomain", null, domainSetup);

				string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
				var baseDir = System.IO.Path.GetDirectoryName(exePath);

				var serverPath = System.IO.Path.Combine(baseDir, "Dwarrowdelf.Server.Engine.dll");

				UpdateStatus("Creating Game");

				var gameFactory = (IGameFactory)m_serverDomain.CreateInstanceFromAndUnwrap(serverPath, "Dwarrowdelf.Server.GameFactory");

				m_game = gameFactory.CreateGame("MyArea", gameDir);
				//m_game = gameFactory.CreateGame("ArenaArea", gameDir);

				UpdateStatus("Game Created");
			}

			void UpdateStatus(string status)
			{
				if (ServerStatusChangeEvent != null)
					ServerStatusChangeEvent(status);
			}

			public void Start(EventWaitHandle serverStartWaitHandle)
			{
				m_serverStartWaitHandle = serverStartWaitHandle;

				m_serverThread = new Thread(Main);
				m_serverThread.Start();
			}

			public void Stop()
			{
				m_game.Stop();
				m_serverThread.Join();
			}

			void Main()
			{
				Thread.CurrentThread.Priority = ThreadPriority.Lowest;
				Thread.CurrentThread.Name = "Main";

				if (m_save == Guid.Empty)
				{
					UpdateStatus("Creating World");
					m_game.CreateWorld();
					UpdateStatus("World Created");
				}
				else
				{
					UpdateStatus("Loading World");
					m_game.LoadWorld(m_save);
					UpdateStatus("World Loaded");
				}

				UpdateStatus("Starting Game");

				m_game.Run(m_serverStartWaitHandle);

				AppDomain.Unload(m_serverDomain);
			}
		}
	}
}
