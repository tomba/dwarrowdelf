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

		public static IntVector3 ToIntVector3(this Vector3 v)
		{
			return new IntVector3(MyMath.Round(v.X), MyMath.Round(v.Y), MyMath.Round(v.Z));
		}

		public static IntVector3 ToFloorIntVector3(this Vector3 v)
		{
			return new IntVector3((int)Math.Floor(v.X), (int)Math.Floor(v.Y), (int)Math.Floor(v.Z));
		}

		public static IntPoint3 ToIntPoint3(this Vector3 v)
		{
			return new IntPoint3(MyMath.Round(v.X), MyMath.Round(v.Y), MyMath.Round(v.Z));
		}

		public static IntPoint3 ToFloorIntPoint3(this Vector3 v)
		{
			return new IntPoint3((int)Math.Floor(v.X), (int)Math.Floor(v.Y), (int)Math.Floor(v.Z));
		}
	}
}
