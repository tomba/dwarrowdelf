using Dwarrowdelf;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	static class MyExtensions
	{
		public static Vector3 ToVector3(this IntVector3 v)
		{
			return new Vector3(v.X, v.Y, v.Z);
		}

		public static Vector3 ToVector3(this IntPoint3 v)
		{
			return new Vector3(v.X, v.Y, v.Z);
		}
	}
}
