using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace Dwarrowdelf
{
	[Serializable]
	public struct IntVector3 : IEquatable<IntVector3>
	{
		// Note: this could be optimized by encoding all values into one int

		readonly int m_x;
		readonly int m_y;
		readonly int m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntVector3(int x, int y, int z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public IntVector3(IntPoint3 p)
		{
			m_x = p.X;
			m_y = p.Y;
			m_z = p.Z;
		}

		public IntVector3(Direction dir)
		{
			Debug.Assert(dir.IsValid());

			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			m_x = (x ^ 1) - 1;
			m_y = (y ^ 1) - 1;
			m_z = (z ^ 1) - 1;
		}

		#region IEquatable<IntVector3> Members

		public bool Equals(IntVector3 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector3))
				return false;

			IntVector3 l = (IntVector3)obj;
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

		public IntVector3 Normalize()
		{
			if (this.IsNull)
				return new IntVector3();

			double len = this.Length;
			var x = (int)Math.Round(this.X / len);
			var y = (int)Math.Round(this.Y / len);
			var z = (int)Math.Round(this.Z / len);
			return new IntVector3(x, y, z);
		}

		public bool IsNormal
		{
			get { return Math.Abs(this.X) <= 1 && Math.Abs(this.Y) <= 1 && Math.Abs(this.Z) <= 1; }
		}

		public static bool operator ==(IntVector3 left, IntVector3 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntVector3 left, IntVector3 right)
		{
			return !(left == right);
		}

		public static IntVector3 operator +(IntVector3 left, IntVector3 right)
		{
			return new IntVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntVector3 operator -(IntVector3 left, IntVector3 right)
		{
			return new IntVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntVector3 operator *(IntVector3 left, int number)
		{
			return new IntVector3(left.X * number, left.Y * number, left.Z * number);
		}

		public static IntVector3 operator /(IntVector3 left, int number)
		{
			return new IntVector3(left.X / number, left.Y / number, left.Z / number);
		}

		public override int GetHashCode()
		{
			return Hash.Hash3D(this.X, this.Y, this.Z);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public static explicit operator IntVector3(IntPoint3 point)
		{
			return new IntVector3(point.X, point.Y, point.Z);
		}

		public Direction ToDirection()
		{
			IntVector3 v = Normalize();

			int d = 0;

			d |= ((v.X + 1) ^ 1) << DirectionConsts.XShift;
			d |= ((v.Y + 1) ^ 1) << DirectionConsts.YShift;
			d |= ((v.Z + 1) ^ 1) << DirectionConsts.ZShift;

			return (Direction)d;
		}

		public IntVector3 Reverse()
		{
			return new IntVector3(-this.X, -this.Y, -this.Z);
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
		public IntVector3 FastRotate(int rotate)
		{
			int x = FastMul(FastCos(rotate), this.X) - FastMul(FastSin(rotate), this.Y);
			int y = FastMul(FastSin(rotate), this.X) + FastMul(FastCos(rotate), this.Y);

			var ix = x > 1 ? 1 : (x < -1 ? -1 : x);
			var iy = y > 1 ? 1 : (y < -1 ? -1 : y);

			return new IntVector3(ix, iy, this.Z);
		}

		public static Direction RotateDir(Direction dir, int rotate)
		{
			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;

			x = (x ^ 1) - 1;
			y = (y ^ 1) - 1;

			var v = new IntVector3(x, y, 0);
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

		public IntVector2 ToIntVector()
		{
			return new IntVector2(this.X, this.Y);
		}
	}
}
