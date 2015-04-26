using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	enum ControlMode
	{
		Fps,
		Rts,
	}

	static class GlobalData
	{
		public static VoxelMap VoxelMap;
		public static ControlMode ControlMode = Client3D.ControlMode.Fps;
		public static bool AlignViewGridToCamera = true;
	}
}
