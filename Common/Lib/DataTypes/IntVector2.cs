using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntVector2Converter))]
	public struct IntVector2 : IEquatable<IntVector2>
	{
		// X, Y: 16 bits, from -32768 to 32767
		// X: bits 0-15, Y: bits 16-31
		int m_value;

		public int X
		{
			get { return (((int)m_value) << 16) >> 16; }
			set { m_value = (m_value & ~0xffff) | (value & 0xffff); }
		}

		public int Y
		{
			get { return ((int)m_value) >> 16; }
			set { m_value = (m_value & ~(0xffff << 16)) | (value & 0xffff) << 16; }
		}

		public IntVector2(int x, int y)
		{
			m_value =
				((x & 0xffff) << 0) |
				((y & 0xffff) << 16);
		}

		public bool IsNull { get { return m_value == 0; } }

		#region IEquatable<IntVector2> Members

		public bool Equals(IntVector2 other)
		{
			return m_value == other.m_value;
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector2))
				return false;

			IntVector2 l = (IntVector2)obj;
			return m_value == l.m_value;
		}

		public double Length
		{
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y); }
		}

		public IntVector2 Normalize()
		{
			double len = this.Length;
			var x = MyMath.Round(this.X / len);
			var y = MyMath.Round(this.Y / len);
			return new IntVector2(x, y);
		}

		public IntVector2 Offset(int offsetX, int offsetY)
		{
			return new IntVector2(this.X + offsetX, this.Y + offsetY);
		}

		public static bool operator ==(IntVector2 left, IntVector2 right)
		{
			return left.m_value == right.m_value;
		}

		public static bool operator !=(IntVector2 left, IntVector2 right)
		{
			return !(left == right);
		}

		public static IntVector2 operator -(IntVector2 v)
		{
			return new IntVector2(-v.X, -v.Y);
		}

		public static IntVector2 operator +(IntVector2 left, IntVector2 right)
		{
			return new IntVector2(left.X + right.X, left.Y + right.Y);
		}

		public static IntVector2 operator -(IntVector2 left, IntVector2 right)
		{
			return new IntVector2(left.X - right.X, left.Y - right.Y);
		}

		public static IntVector2 operator *(IntVector2 left, int right)
		{
			return new IntVector2(left.X * right, left.Y * right);
		}

		public static IntVector2 operator /(IntVector2 left, int number)
		{
			return new IntVector2(left.X / number, left.Y / number);
		}

		public static IntVector2 operator +(IntVector2 left, Direction right)
		{
			int x, y;

			DirectionExtensions.DirectionToComponents(right, out x, out y);

			return new IntVector2(left.X + x, left.Y + y);
		}

		public override int GetHashCode()
		{
			return Hash.Hash2D(this.X, this.Y);
		}

		/// <summary>
		/// Returns a square spiral, centered at center, covering an area of size * size
		/// </summary>
		/// <example>
		/// Size = 5
		/// 016 015 014 013 012
		/// 017 004 003 002 011
		/// 018 005 000 001 010
		/// 019 006 007 008 009
		/// 020 021 022 023 024
		/// </example>
		public static IEnumerable<IntVector2> SquareSpiral(IntVector2 center, int size)
		{
			var p = center;
			var v = new IntVector2(1, 0);

			for (int loop = 0; loop < size * 2 - 1; ++loop)
			{
				for (int i = 0; i < loop / 2 + 1; ++i)
				{
					yield return p;
					p += v;
				}

				v = v.FastRotate(2);
			}
		}

		/// <summary>
		/// Returns a diagonal square spiral
		/// </summary>
		/// <example>
		/// Size = 4
		/// ... ... ... 015 ... ... ...
		/// ... ... 016 006 014 ... ...
		/// ... 017 007 001 005 013 ...
		/// 018 008 002 000 004 012 024
		/// ... 019 009 003 011 023 ...
		/// ... ... 020 010 022 ... ...
		/// ... ... ... 021 ... ... ...
		/// </example>
		/// <param name="center">Center of the spiral</param>
		/// <param name="size">Length of a side of the spiral</param>
		public static IEnumerable<IntVector2> DiagonalSquareSpiral(IntVector2 center, int size)
		{
			var p = center;
			var v = new IntVector2(-1, 1);

			yield return p;

			for (int loop = 1; loop < size; ++loop)
			{
				p += new IntVector2(1, 0);

				for (int t = 0; t < 4; ++t)
				{
					for (int i = 0; i < loop; ++i)
					{
						p += v;
						yield return p;
					}

					v = v.FastRotate(2);
				}
			}
		}

		public Direction ToDirection()
		{
			IntVector2 v = Normalize();

			return DirectionExtensions.ComponentsToDirection(v.X, v.Y);
		}

		public IntVector2 Reverse()
		{
			return new IntVector2(-this.X, -this.Y);
		}

		public IntVector2 Rotate(int angle)
		{
			double rad = Math.PI * angle / 180.0;
			double x = Math.Cos(rad) * this.X - Math.Sin(rad) * this.Y;
			double y = Math.Sin(rad) * this.X + Math.Cos(rad) * this.Y;

			var ix = MyMath.Round(x);
			var iy = MyMath.Round(y);

			return new IntVector2(ix, iy);
		}

		/// <summary>
		/// Return 8 IntVectors pointing to main directions on X-Y plane, each rotated 45 degrees
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<IntVector2> GetAllXYDirections()
		{
			return GetAllXYDirections(Direction.North);
		}

		/// <summary>
		/// Return 8 IntVectors pointing to main directions on X-Y plane, each rotated 45 degrees
		/// </summary>
		/// <param name="startDir">Start direction</param>
		/// <returns></returns>
		public static IEnumerable<IntVector2> GetAllXYDirections(Direction startDir)
		{
			var v = startDir.ToIntVector2();
			for (int i = 0; i < 8; ++i)
			{
				v = v.FastRotate(1);
				yield return v;
			}
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
		public IntVector2 FastRotate(int rotate)
		{
			int x = FastMul(FastCos(rotate), this.X) - FastMul(FastSin(rotate), this.Y);
			int y = FastMul(FastSin(rotate), this.X) + FastMul(FastCos(rotate), this.Y);

			var ix = x > 1 ? 1 : (x < -1 ? -1 : x);
			var iy = y > 1 ? 1 : (y < -1 ? -1 : y);

			return new IntVector2(ix, iy);
		}

		/// <summary>
		/// Rotate dir in 45 degree steps
		/// </summary>
		/// <param name="rotate">Rotation units, in 45 degree steps</param>
		public static Direction RotateDir(Direction dir, int rotate)
		{
			int x, y;

			DirectionExtensions.DirectionToComponents(dir, out x, out y);

			IntVector2 v = new IntVector2(x, y);
			v = v.FastRotate(rotate);
			return v.ToDirection();
		}

		/// <summary>
		/// Rotate the vector in 90 degree steps (positive rotation is clockwise)
		/// </summary>
		public IntVector2 Rotate90(int rotate)
		{
			int x = this.X;
			int y = this.Y;

			if (rotate >= 0)
			{
				for (int i = 0; i < rotate; ++i)
				{
					int tmp = y;
					y = x;
					x = -tmp;
				}
			}
			else
			{
				for (int i = 0; i < -rotate; ++i)
				{
					int tmp = y;
					y = -x;
					x = tmp;
				}
			}

			return new IntVector2(x, y);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1}", this.X, this.Y);
		}

		public static IntVector2 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntVector2(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info));
		}
	}
}
