using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	static class ClientConfig
	{
		static ClientConfig()
		{
			//SaveGamePath = Path.Combine(Win32.SavedGamesFolder.GetSavedGamesPath(), "Dwarrowdelf", "save");
			SaveGamePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "save");
		}

		public static EmbeddedServerMode EmbeddedServerMode = EmbeddedServerMode.SameAppDomain;
		public static ConnectionType ConnectionType = ConnectionType.Tcp;
		public static bool AutoConnect = true;

		public static bool ShowMouseDebug = true;
		public static bool ShowMapDebug = true;

		// Game mode if new game is created
		public static GameOptions NewGameOptions = new GameOptions()
		{
			Mode = GameMode.Adventure,
			Map = GameMap.Fortress,
			TickMethod = WorldTickMethod.Sequential,
		};

		// Delete all saves before starting
		public static bool CleanSaveDir = true;

		public static readonly string SaveGamePath;
	}

	class ClientSavedConfig
	{
		static string ClientSaveFile { get { return System.IO.Path.Combine(ClientConfig.SaveGamePath, "client-config.json"); } }

		public static ClientSavedConfig Load()
		{
			ClientSavedConfig config;

			if (System.IO.File.Exists(ClientSaveFile))
			{
				var dataStr = System.IO.File.ReadAllText(ClientSaveFile);
				config = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientSavedConfig>(dataStr);
			}
			else
			{
				config = new ClientSavedConfig();
			}

			return config;
		}

		public void Save()
		{
			if (!Directory.Exists(ClientConfig.SaveGamePath))
				Directory.CreateDirectory(ClientConfig.SaveGamePath);

			var dataStr = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);

			System.IO.File.WriteAllText(ClientSaveFile, dataStr);
		}

		public Win32.WindowPlacement WindowPlacement { get; set; }
		public bool IsFullScreen { get; set; }
	}
}
