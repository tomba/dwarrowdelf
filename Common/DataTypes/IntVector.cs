using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntVector : IEquatable<IntVector>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }

		public IntVector(int x, int y)
			: this()
		{
			X = x;
			Y = y;
		}

		#region IEquatable<IntVector> Members

		public bool Equals(IntVector l)
		{
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector))
				return false;

			IntVector l = (IntVector)obj;
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		public static bool operator ==(IntVector left, IntVector right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator !=(IntVector left, IntVector right)
		{
			return !(left == right);
		}

		public static IntVector operator +(IntVector left, IntVector right)
		{
			return new IntVector(left.X + right.X, left.Y + right.Y);			
		}

		public static IntVector operator -(IntVector left, IntVector right)
		{
			return new IntVector(left.X - right.X, left.Y - right.Y);
		}

		public override int GetHashCode()
		{
			return (this.X ^ this.Y);
		}

		public override string ToString()
		{
			return String.Format("IntVector({0}, {1})", X, Y);
		}

		public static explicit operator IntVector(IntPoint point)
		{
			return new IntVector(point.X, point.Y);
		}

		public static explicit operator IntSize(IntVector vector)
		{
			return new IntSize(vector.X, vector.Y);
		}

	}

}
