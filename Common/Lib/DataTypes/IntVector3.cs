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
	[System.ComponentModel.TypeConverter(typeof(IntVector3Converter))]
	public struct IntVector3 : IEquatable<IntVector3>
	{
		// X, Y, Z: 16 bits, from -32768 to 32767
		// X: bits 0-15, Y: bits 16-31, Z: bits 48-61
		long m_value;

		public int X
		{
			get { return (((int)m_value) << 16) >> 16; }
			set { m_value = (m_value & ~0xffffL) | (uint)(value & 0xffff); }
		}

		public int Y
		{
			get { return ((int)m_value) >> 16; }
			set { m_value = (m_value & ~(0xffffL << 16)) | ((uint)(value & 0xffff) << 16); }
		}

		public int Z
		{
			get { return (int)(m_value >> 48); }
			set { m_value = (m_value & ~(0xffffL << 48)) | ((long)(value & 0xffff) << 48); }
		}

		public IntVector3(int x, int y, int z)
		{
			m_value =
				((long)(x & 0xffff) << 0) |
				((long)(y & 0xffff) << 16) |
				((long)(z & 0xffff) << 48);
		}

		public IntVector3(IntVector2 p, int z)
			: this(p.X, p.Y, z)
		{
		}

		public int this[int idx]
		{
			get
			{
				switch (idx)
				{
					case 0: return this.X;
					case 1: return this.Y;
					case 2: return this.Z;
					default: throw new IndexOutOfRangeException();
				}
			}

			set
			{
				switch (idx)
				{
					case 0: this.X = value; break;
					case 1: this.Y = value; break;
					case 2: this.Z = value; break;
					default: throw new IndexOutOfRangeException();
				}
			}
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

		public IntVector3 Offset(int offsetX, int offsetY, int offsetZ)
		{
			return new IntVector3(this.X + offsetX, this.Y + offsetY, this.Z + offsetZ);
		}

		public IntVector3 Down { get { return new IntVector3(this.X, this.Y, this.Z - 1); } }
		public IntVector3 Up { get { return new IntVector3(this.X, this.Y, this.Z + 1); } }

		public double Length
		{
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y) + Math.Abs(this.Z); }
		}

		public int ComponentLength
		{
			get { return MyMath.Max(Math.Abs(this.X), Math.Abs(this.Y), Math.Abs(this.Z)); }
		}

		public IntVector3 Normalize()
		{
			if (this.IsNull)
				return new IntVector3();

			double len = this.Length;
			var x = MyMath.Round(this.X / len);
			var y = MyMath.Round(this.Y / len);
			var z = MyMath.Round(this.Z / len);
			return new IntVector3(x, y, z);
		}

		public bool IsNormal
		{
			get { return Math.Abs(this.X) <= 1 && Math.Abs(this.Y) <= 1 && Math.Abs(this.Z) <= 1; }
		}

		public static bool operator ==(IntVector3 left, IntVector3 right)
		{
			return left.m_value == right.m_value;
		}

		public static bool operator !=(IntVector3 left, IntVector3 right)
		{
			return !(left == right);
		}

		public static IntVector3 operator +(IntVector3 left, int right)
		{
			return new IntVector3(left.X + right, left.Y + right, left.Z + right);
		}

		public static IntVector3 operator +(IntVector3 left, IntVector3 right)
		{
			return new IntVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntVector3 operator +(IntVector3 left, IntVector2 right)
		{
			return new IntVector3(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public static IntVector3 operator -(IntVector3 left, int right)
		{
			return new IntVector3(left.X - right, left.Y - right, left.Z - right);
		}

		public static IntVector3 operator -(IntVector3 v)
		{
			return new IntVector3(-v.X, -v.Y, -v.Z);
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

		public static IntVector3 operator +(IntVector3 left, Direction right)
		{
			int x, y, z;

			DirectionExtensions.DirectionToComponents(right, out x, out y, out z);

			return new IntVector3(left.X + x, left.Y + y, left.Z + z);
		}

		public static IntVector3 operator *(IntVector3 left, IntVector3 right)
		{
			return new IntVector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
		}

		public bool IsAdjacentTo(IntVector3 p, DirectionSet positioning)
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

		public static IntVector3 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntVector3(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
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
			IntVector3 v = Normalize();

			return DirectionExtensions.ComponentsToDirection(v.X, v.Y, v.Z);
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
			int x, y;

			DirectionExtensions.DirectionToComponents(dir, out x, out y);

			var v = new IntVector3(x, y, 0);
			v.FastRotate(rotate);
			return v.ToDirection();
		}

		public bool IsNull { get { return m_value == 0; } }

		public IntVector3 Truncate(IntGrid3 box)
		{
			int x = Math.Min(Math.Max(this.X, box.X1), box.X2);
			int y = Math.Min(Math.Max(this.Y, box.Y1), box.Y2);
			int z = Math.Min(Math.Max(this.Z, box.Z1), box.Z2);

			return new IntVector3(x, y, z);
		}

		public IntVector3 SetX(int x)
		{
			return new IntVector3(x, this.Y, this.Z);
		}

		public IntVector3 SetY(int y)
		{
			return new IntVector3(this.X, y, this.Z);
		}

		public IntVector3 SetZ(int z)
		{
			return new IntVector3(this.X, this.Y, z);
		}

		public static IntVector3 Center(IEnumerable<IntVector3> points)
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

			return new IntVector3(x / count, y / count, z / count);
		}

		public static IntVector3 Min(IntVector3 v1, IntVector3 v2)
		{
			return new IntVector3(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y), Math.Min(v1.Z, v2.Z));
		}

		public static IntVector3 Max(IntVector3 v1, IntVector3 v2)
		{
			return new IntVector3(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y), Math.Max(v1.Z, v2.Z));
		}

		public static readonly IntVector3[] AllDirections = DirectionSet.All.ToVectors().ToArray();
		public static readonly IntVector3[] CardinalUpDownDirections = DirectionSet.CardinalUpDown.ToVectors().ToArray();
	}
}
