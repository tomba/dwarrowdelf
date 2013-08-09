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

		public static bool ShowFps = false;
		public static bool ShowMousePos = false;
		public static bool ShowCenterPos = false;
		public static bool ShowTileSize = true;

		// Game mode if new game is created
		public static GameMode NewGameMode = GameMode.Fortress;

		// Delete all saves before starting
		public static bool CleanSaveDir = true;
	}
}
