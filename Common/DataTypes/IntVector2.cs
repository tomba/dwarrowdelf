using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace Dwarrowdelf
{
	[Serializable]
	public struct IntVector2 : IEquatable<IntVector2>
	{
		readonly int m_x;
		readonly int m_y;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }

		public IntVector2(int x, int y)
		{
			m_x = x;
			m_y = y;
		}

		public IntVector2(Direction dir)
		{
			Debug.Assert(dir.IsValid());

			m_x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			m_y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (m_x == DirectionConsts.DirNeg)
				m_x = -1;
			if (m_y == DirectionConsts.DirNeg)
				m_y = -1;
		}

		public bool IsNull
		{
			get
			{
				return this.X == 0 && this.Y == 0;
			}
		}

		#region IEquatable<IntVector2> Members

		public bool Equals(IntVector2 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector2))
				return false;

			IntVector2 l = (IntVector2)obj;
			return ((l.X == this.X) && (l.Y == this.Y));
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
			var x = (int)Math.Round(this.X / len);
			var y = (int)Math.Round(this.Y / len);
			return new IntVector2(x, y);
		}

		public static bool operator ==(IntVector2 left, IntVector2 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator !=(IntVector2 left, IntVector2 right)
		{
			return !(left == right);
		}

		public static IntVector2 operator +(IntVector2 left, IntVector2 right)
		{
			return new IntVector2(left.X + right.X, left.Y + right.Y);
		}

		public static IntVector2 operator -(IntVector2 left, IntVector2 right)
		{
			return new IntVector2(left.X - right.X, left.Y - right.Y);
		}

		public static IntVector2 operator *(IntVector2 left, int number)
		{
			return new IntVector2(left.X * number, left.Y * number);
		}

		public static IntVector2 operator /(IntVector2 left, int number)
		{
			return new IntVector2(left.X / number, left.Y / number);
		}

		public override int GetHashCode()
		{
			return Hash.Hash2D(this.X, this.Y);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1}", this.X, this.Y);
		}

		public static explicit operator IntVector2(IntPoint2 point)
		{
			return new IntVector2(point.X, point.Y);
		}

		public static explicit operator IntSize2(IntVector2 vector)
		{
			return new IntSize2(vector.X, vector.Y);
		}

		public Direction ToDirection()
		{
			IntVector2 v = this.Normalize();

			Direction dir = 0;

			if (v.X > 0)
				dir |= Direction.East;
			else if (v.X < 0)
				dir |= Direction.West;

			if (v.Y > 0)
				dir |= Direction.North;
			else if (v.Y < 0)
				dir |= Direction.South;

			return dir;
		}

		public static IntVector2 FromDirection(Direction dir)
		{
			int x, y;

			x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;
			if (y == DirectionConsts.DirNeg)
				y = -1;

			return new IntVector2(x, y);
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

			var ix = (int)Math.Round(x);
			var iy = (int)Math.Round(y);

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
			var v = FromDirection(startDir);
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

		public static Direction RotateDir(Direction dir, int rotate)
		{
			int x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;

			if (y == DirectionConsts.DirNeg)
				y = -1;

			IntVector2 v = new IntVector2(x, y);
			v = v.FastRotate(rotate);
			return v.ToDirection();
		}

		// XXX needs better name
		public bool IsAdjacent
		{
			get
			{
				if (this.X <= 1 && this.X >= -1 && this.Y <= 1 && this.Y >= -1)
					return true;
				else
					return false;
			}
		}
	}

}
