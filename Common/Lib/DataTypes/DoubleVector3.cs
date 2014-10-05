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

		public DoubleVector3(IntVector2 p, double z)
			: this(p.X, p.Y, z)
		{
		}

		public DoubleVector3(Direction dir)
		{
			int x, y, z;

			DirectionExtensions.DirectionToComponents(dir, out x, out y, out z);

			m_x = x;
			m_y = y;
			m_z = z;
		}

		public bool IsNull { get { return this.X == 0 && this.Y == 0 && this.Z == 0; } }

		#region IEquatable<DoubleVector3> Members

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

		public static DoubleVector3 operator +(DoubleVector3 left, Direction right)
		{
			return left + new DoubleVector3(right);
		}

		public DoubleVector3 Round()
		{
			return new DoubleVector3(MyMath.Round(this.X), MyMath.Round(this.Y), MyMath.Round(this.Z));
		}

		public IntVector3 ToIntVector3()
		{
			return new IntVector3(MyMath.Round(this.X), MyMath.Round(this.Y), MyMath.Round(this.Z));
		}

		public override int GetHashCode()
		{
			throw new Exception("doubles shouldn't be hashed");
			// XXX bad hash
			//return ((int)this.X << 20) | ((int)this.Y << 10) | (int)this.Z;
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public Direction ToDirection()
		{
			DoubleVector3 v = Normalize();

			int d = 0;

			int x = MyMath.Round(v.X);
			int y = MyMath.Round(v.Y);
			int z = MyMath.Round(v.Z);

			return DirectionExtensions.ComponentsToDirection(x, y, z);
		}

		public DoubleVector3 Reverse()
		{
			return new DoubleVector3(-m_x, -m_y, -m_z);
		}
	}
}
