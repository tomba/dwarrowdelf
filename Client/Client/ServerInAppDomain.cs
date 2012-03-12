using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	sealed class ServerInAppDomain
	{
		SaveManager m_saveManager;

		EmbeddedServer m_embeddedServer;

		public event Action<string> StatusChanged;

		public Task StartAsync()
		{
			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path, "Dwarrowdelf", "save");
			if (!System.IO.Directory.Exists(path))
				System.IO.Directory.CreateDirectory(path);

			var gameDir = path;

			bool cleanSaves = true;
			Guid save = Guid.Empty;

			m_saveManager = new SaveManager(gameDir);

			if (cleanSaves)
				m_saveManager.DeleteAll();
			else
				save = m_saveManager.GetLatestSaveFile();

			return Task.Factory.StartNew(() =>
			{
				using (var serverStartWaitHandle = new AutoResetEvent(false))
				{
					m_embeddedServer = new EmbeddedServer(gameDir, save);
					m_embeddedServer.ServerStatusChangeEvent += OnServerStatusChange;
					m_embeddedServer.Start(serverStartWaitHandle);

					var ok = serverStartWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
					if (!ok)
						throw new Exception();
				}
			});
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

		sealed class EmbeddedServer
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
