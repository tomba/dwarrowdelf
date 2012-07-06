using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public struct DoubleVector3 : IEquatable<DoubleVector3>
	{
		readonly double m_x;
		readonly double m_y;
		readonly double m_z;

		public double X { get { return m_x; } }
		public double Y { get { return m_y; } }
		public double Z { get { return m_z; } }

		public DoubleVector3(double x, double y, double z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public DoubleVector3(IntVector3 vector)
		{
			m_x = vector.X;
			m_y = vector.Y;
			m_z = vector.Z;
		}

		public DoubleVector3(Direction dir)
		{
			double x, y, z;

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

		public bool IsNull
		{
			get
			{
				return this.X == 0 && this.Y == 0 && this.Z == 0;
			}
		}

		#region IEquatable<Vector3> Members

		public bool Equals(DoubleVector3 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is DoubleVector3))
				return false;

			DoubleVector3 l = (DoubleVector3)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public double Length
		{
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z); }
		}

		public double SquaredLength
		{
			get { return this.X * this.X + this.Y * this.Y + this.Z * this.Z; }
		}

		public double ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y) + Math.Abs(this.Z); }
		}

		public DoubleVector3 Normalize()
		{
			double len = this.Length;

			if (len == 0)
				return new DoubleVector3();

			return new DoubleVector3(this.X / len, this.Y / len, this.Z / len);
		}

		public bool IsNormal
		{
			get { return this.Length == 1; }
		}

		public static bool operator ==(DoubleVector3 left, DoubleVector3 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(DoubleVector3 left, DoubleVector3 right)
		{
			return !(left == right);
		}

		public static DoubleVector3 operator +(DoubleVector3 left, DoubleVector3 right)
		{
			return new DoubleVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static DoubleVector3 operator -(DoubleVector3 left, DoubleVector3 right)
		{
			return new DoubleVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static DoubleVector3 operator *(DoubleVector3 v, double number)
		{
			return new DoubleVector3(v.X * number, v.Y * number, v.Z * number);
		}

		public static DoubleVector3 operator /(DoubleVector3 v, double number)
		{
			return new DoubleVector3(v.X / number, v.Y / number, v.Z / number);
		}

		public IntVector3 ToIntVector3()
		{
			return new IntVector3((int)Math.Round(this.X), (int)Math.Round(this.Y), (int)Math.Round(this.Z));
		}

		public override int GetHashCode()
		{
			// XXX bad hash
			return ((int)this.X << 20) | ((int)this.Y << 10) | (int)this.Z;
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public Direction ToDirection()
		{
			DoubleVector3 v = Normalize();

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

		public DoubleVector3 Reverse()
		{
			return new DoubleVector3(-m_x, -m_y, -m_z);
		}
	}
}
