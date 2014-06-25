using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dwarrowdelf
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct IntVector3Mutable : IEquatable<IntVector3Mutable>
	{
		// X, Y, Z: 16 bits, from -32768 to 32767
		// X: bits 0-15, Y: bits 16-31, Z: bits 48-61
		[FieldOffset(0)]
		long m_value;

		[FieldOffset(0)]
		short m_s1;
		[FieldOffset(2)]
		short m_s2;
		[FieldOffset(6)]
		short m_s3;


		public int X { get { return (((int)m_value) << 16) >> 16; } }
		public int Y { get { return ((int)m_value) >> 16; } }
		public int Z { get { return (int)(m_value >> 48); } }

		public IntVector3Mutable(int x, int y, int z)
		{
			m_s1 = m_s2 = m_s3 = 0;

			m_value =
				((long)(x & 0xffff) << 0) |
				((long)(y & 0xffff) << 16) |
				((long)(z & 0xffff) << 48);
		}

		public IntVector3Mutable(IntVector2 p, int z)
			: this(p.X, p.Y, z)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(int x, int y, int z)
		{
			m_s1 += (short)x;
			m_s2 += (short)y;
			m_s3 += (short)z;
			/*
			var xx = (m_value1 & 0xffff) + (uint)x;

			m_value1 = (xx) |
				((m_value1 & 0xffff0000) + ((uint)y << 16));

			m_value2 = (uint)z;
			*/
			/*
			x += (int)m_value & 0xffff;
			y += (int)(m_value >> 16) & 0xffff;
			z += (int)(m_value >> 48) & 0xffff;

			m_value =
				((long)(x & 0xffff) << 0) |
				((long)(y & 0xffff) << 16) |
				((long)(z & 0xffff) << 48);
			 */
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IntVector3Mutable(Direction dir)
		{
			m_s1 = m_s2 = m_s3 = 0;

			Debug.Assert(dir.IsValid());

			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			x = ((x ^ 1) - (x >> 1)) - 1;
			y = ((y ^ 1) - (y >> 1)) - 1;
			z = ((z ^ 1) - (z >> 1)) - 1;

			m_value =
				((long)(x & 0xffff) << 0) |
				((long)(y & 0xffff) << 16) |
				((long)(z & 0xffff) << 48);
		}

		#region IEquatable<IntVector3Mutable> Members

		public bool Equals(IntVector3Mutable other)
		{
			return other.m_value == this.m_value;
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector3Mutable))
				return false;

			IntVector3Mutable l = (IntVector3Mutable)obj;
			return l.m_value == this.m_value;
		}

		public IntVector3Mutable Offset(int offsetX, int offsetY, int offsetZ)
		{
			return new IntVector3Mutable(this.X + offsetX, this.Y + offsetY, this.Z + offsetZ);
		}

		public IntVector3Mutable Down { get { return new IntVector3Mutable(this.X, this.Y, this.Z - 1); } }
		public IntVector3Mutable Up { get { return new IntVector3Mutable(this.X, this.Y, this.Z + 1); } }

		public double Length
		{
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y) + Math.Abs(this.Z); }
		}

		public IntVector3Mutable Normalize()
		{
			if (this.IsNull)
				return new IntVector3Mutable();

			double len = this.Length;
			var x = MyMath.Round(this.X / len);
			var y = MyMath.Round(this.Y / len);
			var z = MyMath.Round(this.Z / len);
			return new IntVector3Mutable(x, y, z);
		}

		public bool IsNormal
		{
			get { return Math.Abs(this.X) <= 1 && Math.Abs(this.Y) <= 1 && Math.Abs(this.Z) <= 1; }
		}

		public static bool operator ==(IntVector3Mutable left, IntVector3Mutable right)
		{
			return left.m_value == right.m_value;
		}

		public static bool operator !=(IntVector3Mutable left, IntVector3Mutable right)
		{
			return !(left == right);
		}

		public static IntVector3Mutable operator +(IntVector3Mutable left, IntVector3Mutable right)
		{
			return new IntVector3Mutable(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntVector3Mutable operator +(IntVector3Mutable left, IntVector2 right)
		{
			return new IntVector3Mutable(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public static IntVector3Mutable operator -(IntVector3Mutable left, IntVector3Mutable right)
		{
			return new IntVector3Mutable(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntVector3Mutable operator *(IntVector3Mutable left, int number)
		{
			return new IntVector3Mutable(left.X * number, left.Y * number, left.Z * number);
		}

		public static IntVector3Mutable operator /(IntVector3Mutable left, int number)
		{
			return new IntVector3Mutable(left.X / number, left.Y / number, left.Z / number);
		}

		public static IntVector3Mutable operator +(IntVector3Mutable left, Direction right)
		{
			return left + new IntVector3Mutable(right);
		}

		public bool IsAdjacentTo(IntVector3Mutable p, DirectionSet positioning)
		{
			var v = p - this;

			if (!v.IsNormal)
				return false;

			var d = v.ToDirection();

			return positioning.Contains(d);
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

		public static IntVector3Mutable Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntVector3Mutable(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
		}

		public IntVector2 ToIntVector2()
		{
			return new IntVector2(this.X, this.Y);
		}

		public DoubleVector3 ToDoubleVector3()
		{
			return new DoubleVector3(this.X, this.Y, this.Z);
		}

		public Direction ToDirection()
		{
			IntVector3Mutable v = Normalize();

			int d = 0;

			int x = ((v.X + 1) ^ 1) - ((v.X + 1) >> 1);
			int y = ((v.Y + 1) ^ 1) - ((v.Y + 1) >> 1);
			int z = ((v.Z + 1) ^ 1) - ((v.Z + 1) >> 1);

			d |= x << DirectionConsts.XShift;
			d |= y << DirectionConsts.YShift;
			d |= z << DirectionConsts.ZShift;

			return (Direction)d;
		}

		public IntVector3Mutable Reverse()
		{
			return new IntVector3Mutable(-this.X, -this.Y, -this.Z);
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
		public IntVector3Mutable FastRotate(int rotate)
		{
			int x = FastMul(FastCos(rotate), this.X) - FastMul(FastSin(rotate), this.Y);
			int y = FastMul(FastSin(rotate), this.X) + FastMul(FastCos(rotate), this.Y);

			var ix = x > 1 ? 1 : (x < -1 ? -1 : x);
			var iy = y > 1 ? 1 : (y < -1 ? -1 : y);

			return new IntVector3Mutable(ix, iy, this.Z);
		}

		public static Direction RotateDir(Direction dir, int rotate)
		{
			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;

			x = ((x ^ 1) - (x >> 1)) - 1;
			y = ((y ^ 1) - (y >> 1)) - 1;

			var v = new IntVector3Mutable(x, y, 0);
			v.FastRotate(rotate);
			return v.ToDirection();
		}

		public bool IsNull { get { return m_value == 0; } }

		public IntVector3Mutable Truncate(IntGrid3 box)
		{
			int x = Math.Min(Math.Max(this.X, box.X1), box.X2);
			int y = Math.Min(Math.Max(this.Y, box.Y1), box.Y2);
			int z = Math.Min(Math.Max(this.Z, box.Z1), box.Z2);

			return new IntVector3Mutable(x, y, z);
		}

		public IntVector3Mutable SetX(int x)
		{
			return new IntVector3Mutable(x, this.Y, this.Z);
		}

		public IntVector3Mutable SetY(int y)
		{
			return new IntVector3Mutable(this.X, y, this.Z);
		}

		public IntVector3Mutable SetZ(int z)
		{
			return new IntVector3Mutable(this.X, this.Y, z);
		}

		public static IntVector3Mutable Center(IEnumerable<IntVector3Mutable> points)
		{
			int x, y, z;
			int count = 0;
			x = y = z = 0;

			foreach (var p in points)
			{
				x += p.X;
				y += p.Y;
				z += p.Z;
				count++;
			}

			return new IntVector3Mutable(x / count, y / count, z / count);
		}
	}
}
