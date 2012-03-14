using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	sealed class EmbeddedServer
	{
		SaveManager m_saveManager;

		public event Action<string> StatusChanged;

		IGame m_game;
		Thread m_serverThread;
		AppDomain m_serverDomain;
		Guid m_save;

		public Task StartAsync()
		{
			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.None)
				throw new Exception();

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
					CreateEmbeddedServer(gameDir, save);

					m_serverThread = new Thread(ServerMain);
					m_serverThread.Start(serverStartWaitHandle);

					var ok = serverStartWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
					if (!ok)
						throw new Exception();
				}
			});
		}

		public void Stop()
		{
			m_game.Stop();
			m_serverThread.Join();
		}

		void OnServerStatusChange(string status)
		{
			if (this.StatusChanged != null)
				this.StatusChanged(status);
		}

		void UpdateStatus(string status)
		{
			if (StatusChanged != null)
				StatusChanged(status);
		}

		void CreateEmbeddedServer(string gameDir, Guid save)
		{
			m_save = save;

			string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var baseDir = System.IO.Path.GetDirectoryName(exePath);
			var serverPath = System.IO.Path.Combine(baseDir, "Dwarrowdelf.Server.Engine.dll");

			AppDomain appDomain;

			if (ClientConfig.EmbeddedServer == EmbeddedServerMode.SeparateAppDomain)
			{
				var di = AppDomain.CurrentDomain.SetupInformation;

				var domainSetup = new AppDomainSetup()
				{
					ApplicationBase = di.ApplicationBase,
					ConfigurationFile = di.ApplicationBase + "Dwarrowdelf.Server.exe.config",
				};

				m_serverDomain = AppDomain.CreateDomain("ServerDomain", null, domainSetup);

				appDomain = m_serverDomain;

				UpdateStatus("Creating Game");

			}
			else
			{
				appDomain = AppDomain.CurrentDomain;
			}

			var gameFactory = (IGameFactory)appDomain.CreateInstanceFromAndUnwrap(serverPath, "Dwarrowdelf.Server.GameFactory");
			m_game = gameFactory.CreateGame("MyArea", gameDir);
			//m_game = gameFactory.CreateGame("ArenaArea", gameDir);

			UpdateStatus("Game Created");
		}

		void ServerMain(object arg)
		{
			EventWaitHandle serverStartWaitHandle = (EventWaitHandle)arg;

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

			m_game.Run(serverStartWaitHandle);

			if (m_serverDomain != null)
				AppDomain.Unload(m_serverDomain);
		}
	}
}
