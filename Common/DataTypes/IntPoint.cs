using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntPoint : IEquatable<IntPoint>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }

		public IntPoint(int x, int y)
			: this()
		{
			X = x;
			Y = y;
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

		public void Offset(int offsetX, int offsetY)
		{
			this.X += offsetX;
			this.Y += offsetY;
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
