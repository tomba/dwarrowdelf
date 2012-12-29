using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public struct DoublePoint3 : IEquatable<DoublePoint3>
	{
		readonly double m_x;
		readonly double m_y;
		readonly double m_z;

		public double X { get { return m_x; } }
		public double Y { get { return m_y; } }
		public double Z { get { return m_z; } }

		public DoublePoint3(double x, double y, double z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public DoublePoint3(IntPoint2 p, double z)
			: this(p.X, p.Y, z)
		{
		}

		#region IEquatable<DoublePoint3> Members

		public bool Equals(DoublePoint3 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is DoublePoint3))
				return false;

			DoublePoint3 l = (DoublePoint3)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(DoublePoint3 left, DoublePoint3 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(DoublePoint3 left, DoublePoint3 right)
		{
			return !(left == right);
		}

		public static DoublePoint3 operator +(DoublePoint3 left, DoubleVector3 right)
		{
			return new DoublePoint3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static DoublePoint3 operator -(DoublePoint3 left, DoubleVector3 right)
		{
			return new DoublePoint3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static DoubleVector3 operator -(DoublePoint3 left, DoublePoint3 right)
		{
			return new DoubleVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
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
	}
}
