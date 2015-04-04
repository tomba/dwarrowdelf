using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	static class ClientConfig
	{
		public static EmbeddedServerMode EmbeddedServerMode = EmbeddedServerMode.SameAppDomain;
		public static ConnectionType ConnectionType = ConnectionType.Tcp;
		public static bool AutoConnect = true;

		public static bool ShowMouseDebug = true;
		public static bool ShowMapDebug = true;

		// Game mode if new game is created
		public static GameOptions NewGameOptions = new GameOptions()
		{
			Mode = GameMode.Fortress,
			Map = GameMap.Fortress,
			TickMethod = WorldTickMethod.Simultaneous,
		};

		// Delete all saves before starting
		public static bool CleanSaveDir = true;

		public static readonly string SaveGamePath = System.IO.Path.Combine(Win32.SavedGamesFolder.GetSavedGamesPath(), "Dwarrowdelf", "save");
	}
}
