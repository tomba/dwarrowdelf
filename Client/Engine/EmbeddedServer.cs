using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	public enum EmbeddedServerMode
	{
		None,
		SameAppDomain,
		SeparateAppDomain,
	}

	public class EmbeddedServerOptions
	{
		public EmbeddedServerMode ServerMode;
		public GameMode NewGameMode;
		public string SaveGamePath;
		public bool CleanSaveDir;
	}

	public sealed class EmbeddedServer
	{
		EmbeddedServerMode m_serverMode;
		GameMode m_gameMode;

		SaveManager m_saveManager;

		public event Action<string> StatusChanged;

		IGameFactory m_gameFactory;
		IGame m_game;
		Thread m_serverThread;
		AppDomain m_serverDomain;
		Guid m_save;

		public IGame Game { get { return m_game; } }

		public Task StartAsync(EmbeddedServerOptions options)
		{
			if (options.ServerMode == EmbeddedServerMode.None)
				throw new Exception();

			m_serverMode = options.ServerMode;
			m_gameMode = options.NewGameMode;

			if (!System.IO.Directory.Exists(options.SaveGamePath))
				System.IO.Directory.CreateDirectory(options.SaveGamePath);

			Guid save = Guid.Empty;

			m_saveManager = new SaveManager(options.SaveGamePath);

			if (options.CleanSaveDir)
				m_saveManager.DeleteAll();
			else
				save = m_saveManager.GetLatestSaveFile();

			CreateEmbeddedServer(options.SaveGamePath, save);

			var tcs = new TaskCompletionSource<object>();

			var serverStartWaitHandle = new AutoResetEvent(false);

			ThreadPool.RegisterWaitForSingleObject(serverStartWaitHandle,
				(o, timeout) =>
				{
					serverStartWaitHandle.Dispose();

					if (timeout)
					{
						m_serverThread.Abort();
						tcs.SetException(new Exception("Timeout waiting for server"));
					}
					else
					{
						tcs.SetResult(null);
					}
				},
				null, TimeSpan.FromSeconds(60), true);

			m_serverThread = new Thread(ServerMain);
			m_serverThread.Start(serverStartWaitHandle);

			return tcs.Task;
		}

		public Task StopAsync()
		{
			return Task.Run(() =>
			{
				m_game.Stop();
				m_serverThread.Join();
			});
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
			var serverPath = System.IO.Path.Combine(baseDir, "Dwarrowdelf.Server.exe");

			AppDomain appDomain;

			switch (m_serverMode)
			{
				case EmbeddedServerMode.SeparateAppDomain:
					{
						var di = AppDomain.CurrentDomain.SetupInformation;

						var domainSetup = new AppDomainSetup()
						{
							ApplicationBase = di.ApplicationBase,
							ConfigurationFile = di.ApplicationBase + "Dwarrowdelf.Server.exe.config",
						};

						m_serverDomain = AppDomain.CreateDomain("ServerDomain", null, domainSetup);

						appDomain = m_serverDomain;
					}
					break;

				case EmbeddedServerMode.SameAppDomain:
					appDomain = AppDomain.CurrentDomain;
					break;

				default:
					throw new Exception();
			}

			m_gameFactory = (IGameFactory)appDomain.CreateInstanceFromAndUnwrap(serverPath, "Dwarrowdelf.Server.GameFactory");
		}

		void ServerMain(object arg)
		{
			EventWaitHandle serverStartWaitHandle = (EventWaitHandle)arg;

			Thread.CurrentThread.Priority = ThreadPriority.Lowest;
			Thread.CurrentThread.Name = "SMain";

			if (m_save == Guid.Empty)
			{
				UpdateStatus("Creating Game");

				GameMode gameMode;
				GameMap gameMap;

				switch (m_gameMode)
				{
					case GameMode.Fortress:
						gameMode = GameMode.Fortress;
						gameMap = GameMap.Fortress;
						break;

					case GameMode.Adventure:
						gameMode = GameMode.Adventure;
						gameMap = GameMap.Adventure;
						break;

					default:
						throw new Exception();
				}

				m_game = m_gameFactory.CreateGame(m_saveManager.GameDir, gameMode, gameMap);
				UpdateStatus("Game Created");
			}
			else
			{
				UpdateStatus("Loading Game");
				m_game = m_gameFactory.LoadGame(m_saveManager.GameDir, m_save);
				UpdateStatus("Game Loaded");
			}

			UpdateStatus("Starting Game");

			m_game.Run(serverStartWaitHandle);

			if (m_serverDomain != null)
				AppDomain.Unload(m_serverDomain);
		}
	}
}
