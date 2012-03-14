using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	enum EmbeddedServerMode
	{
		None,
		SameAppDomain,
		SeparateAppDomain,
	}

	static class ClientConfig
	{
		public static EmbeddedServerMode EmbeddedServer = EmbeddedServerMode.SeparateAppDomain;
		public static bool AutoConnect = true;

		public static bool ShowFps = false;
		public static bool ShowMousePos = false;
		public static bool ShowCenterPos = false;

	}
}
