﻿
#define USE_3INTS		// 96 bits
//#define USE_3SHORTS	// 48 bits
//#define USE_1INT		// 32 bits
//#define USE_1LONG		// 64 bits
//#define USE_2INTS		// 64 bits

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntPoint3Converter))]
	public struct IntPoint3 : IEquatable<IntPoint3>
	{
#if USE_3INTS
		readonly int m_x;
		readonly int m_y;
		readonly int m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntPoint3(int x, int y, int z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}
#elif USE_3SHORTS
		readonly short m_x;
		readonly short m_y;
		readonly short m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntPoint3(int x, int y, int z)
		{
			m_x = (short)x;
			m_y = (short)y;
			m_z = (short)z;
		}
#elif USE_1INT
		readonly int m_value;

		// X: 12 bits, from -2048 to 2047
		// Y: 12 bits, from -2048 to 2047
		// Z: 8 bits, from -128 to 127
		const int x_width = 12;
		const int y_width = 12;
		const int z_width = 8;
		const int x_mask = (1 << x_width) - 1;
		const int y_mask = (1 << y_width) - 1;
		const int z_mask = (1 << z_width) - 1;
		const int x_shift = 0;
		const int y_shift = x_width;
		const int z_shift = x_width + y_width;
		const int xyz_width = 32;

		public int X { get { return (m_value << (xyz_width - x_width - x_shift)) >> (xyz_width - x_width); } }
		public int Y { get { return (m_value << (xyz_width - y_width - y_shift)) >> (xyz_width - y_width); } }
		public int Z { get { return (m_value << (xyz_width - z_width - z_shift)) >> (xyz_width - z_width); } }

		public IntPoint3(int x, int y, int z)
		{
			m_value =
				((x & x_mask) << x_shift) |
				((y & y_mask) << y_shift) |
				((z & z_mask) << z_shift);

			System.Diagnostics.Debug.Assert(x == this.X && y == this.Y && z == this.Z);
		}
#elif USE_1LONG
		readonly ulong m_value;

		// X: 24 bits, from -8388608 to 8388607
		// Y: 24 bits, from -8388608 to 8388607
		// Z: 16 bits, from -32768 to 32767
		const int x_width = 24;
		const int y_width = 24;
		const int z_width = 16;
		const int x_mask = (1 << x_width) - 1;
		const int y_mask = (1 << y_width) - 1;
		const int z_mask = (1 << z_width) - 1;
		const int x_shift = 0;
		const int y_shift = x_width;
		const int z_shift = x_width + y_width;
		const int xyz_width = 64;

		public int X { get { return (int)((m_value << (xyz_width - x_width - x_shift)) >> (xyz_width - x_width)); } }
		public int Y { get { return (int)((m_value << (xyz_width - y_width - y_shift)) >> (xyz_width - y_width)); } }
		public int Z { get { return (int)((m_value << (xyz_width - z_width - z_shift)) >> (xyz_width - z_width)); } }

		public IntPoint3(int x, int y, int z)
		{
			m_value =
				((ulong)(x & x_mask) << x_shift) |
				((ulong)(y & y_mask) << y_shift) |
				((ulong)(z & z_mask) << z_shift);

			System.Diagnostics.Debug.Assert(x == this.X && y == this.Y && z == this.Z);
		}
#elif USE_2INTS
		readonly int m_value1;
		readonly int m_value2;

		// X: 32 bits
		// Y: 24 bits
		// Z: 8 bits
		const int y_width = 24;
		const int z_width = 8;
		const int y_mask = (1 << y_width) - 1;
		const int z_mask = (1 << z_width) - 1;
		const int z_shift = 24;

		public int X { get { return m_value1; } }
		public int Y { get { return (m_value2 << 8) >> 8; } }
		public int Z { get { return m_value2 >> 24; } }

		public IntPoint3(int x, int y, int z)
		{
			m_value1 = x;
			m_value2 = (y & y_mask) | ((z & z_mask) << z_shift);

			System.Diagnostics.Debug.Assert(x == this.X && y == this.Y && z == this.Z);
		}
#else
#error no operating mode for IntPoint3
#endif

		public IntPoint3(IntPoint2 p, int z)
			: this(p.X, p.Y, z)
		{
		}

		#region IEquatable<IntPoint3> Members

		public bool Equals(IntPoint3 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint3))
				return false;

			IntPoint3 l = (IntPoint3)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(IntPoint3 left, IntPoint3 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntPoint3 left, IntPoint3 right)
		{
			return !(left == right);
		}

		public static IntPoint3 operator +(IntPoint3 left, IntVector3 right)
		{
			return new IntPoint3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntPoint3 operator -(IntPoint3 left, IntVector3 right)
		{
			return new IntPoint3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3 operator +(IntPoint3 left, Direction right)
		{
			return left + new IntVector3(right);
		}

		public static IntVector3 operator -(IntPoint3 left, IntPoint3 right)
		{
			return new IntVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3 operator +(IntPoint3 left, IntVector2 right)
		{
			return new IntPoint3(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public bool IsAdjacentTo(IntPoint3 p, DirectionSet positioning)
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

		public IntPoint2 ToIntPoint()
		{
			return new IntPoint2(this.X, this.Y);
		}

		public IntPoint3 Truncate(IntGrid3 box)
		{
			int x = Math.Min(Math.Max(this.X, box.X1), box.X2);
			int y = Math.Min(Math.Max(this.Y, box.Y1), box.Y2);
			int z = Math.Min(Math.Max(this.Z, box.Z1), box.Z2);

			return new IntPoint3(x, y, z);
		}

		public static IntPoint3 Center(IEnumerable<IntPoint3> points)
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

			return new IntPoint3(x / count, y / count, z / count);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public static IntPoint3 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntPoint3(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
		}
	}
}