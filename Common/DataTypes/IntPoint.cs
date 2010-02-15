using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public struct IntPoint : IEquatable<IntPoint>
	{
		[DataMember(Name = "X")]
		readonly int m_x;
		[DataMember(Name = "Y")]
		readonly int m_y;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }

		public IntPoint(int x, int y)
		{
			m_x = x;
			m_y = y;
		}

		#region IEquatable<IntPoint> Members

		public bool Equals(IntPoint other)
		{
			return ((other.X == this.X) && (other.Y == this.Y));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint))
				return false;

			IntPoint l = (IntPoint)obj;
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		public IntPoint Offset(int offsetX, int offsetY)
		{
			return new IntPoint(this.X + offsetX, this.Y + offsetY);
		}

		public static bool operator ==(IntPoint left, IntPoint right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator !=(IntPoint left, IntPoint right)
		{
			return !(left == right);
		}

		
		public static IntPoint operator +(IntPoint left, IntVector right)
		{
			return new IntPoint(left.X + right.X, left.Y + right.Y);			
		}

		public static IntVector operator -(IntPoint left, IntPoint right)
		{
			return new IntVector(left.X - right.X, left.Y - right.Y);
		}

		public static IntPoint operator -(IntPoint left, IntVector right)
		{
			return new IntPoint(left.X - right.X, left.Y - right.Y);
		}

		public static IntPoint operator *(IntPoint left, int right)
		{
			return new IntPoint(left.X * right, left.Y * right);
		}

		public override int GetHashCode()
		{
			return (this.X << 16) | this.Y;
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture, "IntPoint({0}, {1})", X, Y);
		}

		public static explicit operator IntPoint(IntVector vector)
		{
			return new IntPoint(vector.X, vector.Y);
		}

	}

}
