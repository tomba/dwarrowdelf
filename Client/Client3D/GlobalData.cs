using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	enum ControlMode
	{
		Fps,
		Rts,
	}

	static class GlobalData
	{
		public static Map Map;
		public static ControlMode ControlMode = ControlMode.Fps;
		public static bool AlignViewGridToCamera = true;
	}
}
