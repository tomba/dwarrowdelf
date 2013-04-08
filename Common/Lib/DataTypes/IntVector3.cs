using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dwarrowdelf
{
	[Serializable]
	public struct IntVector3 : IEquatable<IntVector3>
	{
		readonly long m_value;

		// X: 24 bits, from -8388608 to 8388607
		// Y: 24 bits, from -8388608 to 8388607
		// Z: 16 bits, from -32768 to 32767
		const int x_width = 24;
		const int y_width = 24;
		const int z_width = 16;
		const int xyz_width = 64;
		const int x_mask = (1 << x_width) - 1;
		const int y_mask = (1 << y_width) - 1;
		const int z_mask = (1 << z_width) - 1;
		const int x_shift = 0;
		const int y_shift = x_width;
		const int z_shift = x_width + y_width;

		public int X { get { return (int)((m_value << (xyz_width - x_width - x_shift)) >> (xyz_width - x_width)); } }
		public int Y { get { return (int)((m_value << (xyz_width - y_width - y_shift)) >> (xyz_width - y_width)); } }
		public int Z { get { return (int)((m_value << (xyz_width - z_width - z_shift)) >> (xyz_width - z_width)); } }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IntVector3(int x, int y, int z)
		{
			m_value =
				((long)(x & x_mask) << x_shift) |
				((long)(y & y_mask) << y_shift) |
				((long)(z & z_mask) << z_shift);
		}

		public IntVector3(IntPoint3 p)
			: this(p.X, p.Y, p.Z)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IntVector3(Direction dir)
		{
			Debug.Assert(dir.IsValid());

			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			x = ((x ^ 1) - (x >> 1)) - 1;
			y = ((y ^ 1) - (y >> 1)) - 1;
			z = ((z ^ 1) - (z >> 1)) - 1;

			m_value =
				((long)(x & x_mask) << x_shift) |
				((long)(y & y_mask) << y_shift) |
				((long)(z & z_mask) << z_shift);
		}

		#region IEquatable<IntVector3> Members

		public bool Equals(IntVector3 other)
		{
			return other.m_value == this.m_value;
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector3))
				return false;

			IntVector3 l = (IntVector3)obj;
			return l.m_value == this.m_value;
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

			int x = ((v.X + 1) ^ 1) - ((v.X + 1) >> 1);
			int y = ((v.Y + 1) ^ 1) - ((v.Y + 1) >> 1);
			int z = ((v.Z + 1) ^ 1) - ((v.Z + 1) >> 1);

			d |= x << DirectionConsts.XShift;
			d |= y << DirectionConsts.YShift;
			d |= z << DirectionConsts.ZShift;

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

			x = ((x ^ 1) - (x >> 1)) - 1;
			y = ((y ^ 1) - (y >> 1)) - 1;

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
