using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public struct IntVector3D : IEquatable<IntVector3D>
	{
		readonly int m_x;
		readonly int m_y;
		readonly int m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntVector3D(int x, int y, int z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public IntVector3D(Direction dir)
		{
			int x, y, z;

			x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;
			z = ((int)dir >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;
			if (y == DirectionConsts.DirNeg)
				y = -1;
			if (z == DirectionConsts.DirNeg)
				z = -1;

			m_x = x;
			m_y = y;
			m_z = z;
		}

		#region IEquatable<IntVector3D> Members

		public bool Equals(IntVector3D other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector3D))
				return false;

			IntVector3D l = (IntVector3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public double Length
		{
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y) + Math.Abs(this.Z); }
		}

		public IntVector3D Normalize()
		{
			if (this.IsNull)
				return new IntVector3D();

			double len = this.Length;
			var x = (int)Math.Round(this.X / len);
			var y = (int)Math.Round(this.Y / len);
			var z = (int)Math.Round(this.Z / len);
			return new IntVector3D(x, y, z);
		}

		public bool IsNormal
		{
			get { return Math.Abs(this.X) <= 1 && Math.Abs(this.Y) <= 1 && Math.Abs(this.Z) <= 1; }
		}

		public static bool operator ==(IntVector3D left, IntVector3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntVector3D left, IntVector3D right)
		{
			return !(left == right);
		}

		public static IntVector3D operator +(IntVector3D left, IntVector3D right)
		{
			return new IntVector3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntVector3D operator -(IntVector3D left, IntVector3D right)
		{
			return new IntVector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntVector3D operator *(IntVector3D left, int number)
		{
			return new IntVector3D(left.X * number, left.Y * number, left.Z * number);
		}

		public override int GetHashCode()
		{
			return (this.X << 20) | (this.Y << 10) | this.Z;
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public static explicit operator IntVector3D(IntPoint3D point)
		{
			return new IntVector3D(point.X, point.Y, point.Z);
		}

		public Direction ToDirection()
		{
			IntVector3D v = Normalize();

			Direction dir = 0;

			if (v.X > 0)
				dir |= Direction.East;
			else if (v.X < 0)
				dir |= Direction.West;

			if (v.Y > 0)
				dir |= Direction.North;
			else if (v.Y < 0)
				dir |= Direction.South;

			if (v.Z > 0)
				dir |= Direction.Up;
			else if (v.Z < 0)
				dir |= Direction.Down;

			return dir;
		}

		public IntVector3D Reverse()
		{
			return new IntVector3D(-m_x, -m_y, -m_z);
		}

		static int FastCos(int rot)
		{
			rot %= 8;
			if (rot < 0)
				rot += 8;
			if (rot == 0 || rot == 1 || rot == 7)
				return 1;
			if (rot == 2 || rot == 6)
				return 0;
			return -1;
		}

		static int FastSin(int rot)
		{
			rot %= 8;
			if (rot < 0)
				rot += 8;
			if (rot == 1 || rot == 2 || rot == 3)
				return 1;
			if (rot == 0 || rot == 4)
				return 0;
			return -1;
		}

		static int FastMul(int a, int b)
		{
			if (a == 0 || b == 0)
				return 0;
			if (a == b)
				return 1;
			return -1;
		}

		/// <summary>
		/// Rotate unit vector in 45 degree steps
		/// </summary>
		/// <param name="rotate">Rotation units, in 45 degree steps</param>
		public IntVector3D FastRotate(int rotate)
		{
			int x = FastMul(FastCos(rotate), this.X) - FastMul(FastSin(rotate), this.Y);
			int y = FastMul(FastSin(rotate), this.X) + FastMul(FastCos(rotate), this.Y);

			var ix = x > 1 ? 1 : (x < -1 ? -1 : x);
			var iy = y > 1 ? 1 : (y < -1 ? -1 : y);

			return new IntVector3D(ix, iy, this.Z);
		}

		public static Direction RotateDir(Direction dir, int rotate)
		{
			int x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;

			if (y == DirectionConsts.DirNeg)
				y = -1;

			var v = new IntVector3D(x, y, 0);
			v.FastRotate(rotate);
			return v.ToDirection();
		}

		public bool IsNull
		{
			get
			{
				return this.X == 0 && this.Y == 0 && this.Z == 0;
			}
		}

		public IntVector ToIntVector()
		{
			return new IntVector(this.X, this.Y);
		}
	}
}
