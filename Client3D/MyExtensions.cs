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

		public static IntVector3 ToIntVector3(this Vector3 v)
		{
			return new IntVector3(MyMath.Round(v.X), MyMath.Round(v.Y), MyMath.Round(v.Z));
		}

		public static IntVector3 ToFloorIntVector3(this Vector3 v)
		{
			return new IntVector3((int)Math.Floor(v.X), (int)Math.Floor(v.Y), (int)Math.Floor(v.Z));
		}

		public static FaceDirectionBits ToFaceDirectionBits(this Direction dir)
		{
			switch (dir)
			{
				case Direction.East:
					return FaceDirectionBits.PositiveX;
				case Direction.West:
					return FaceDirectionBits.NegativeX;
				case Direction.South:
					return FaceDirectionBits.PositiveY;
				case Direction.North:
					return FaceDirectionBits.NegativeY;
				case Direction.Up:
					return FaceDirectionBits.PositiveZ;
				case Direction.Down:
					return FaceDirectionBits.NegativeZ;
				default:
					throw new Exception();
			}
		}
	}
}
