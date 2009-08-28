using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntPoint3D : IEquatable<IntPoint3D>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }
		[DataMember]
		public int Z { get; set; }

		public IntPoint3D(int x, int y, int z)
			: this()
		{
			X = x;
			Y = y;
			Z = z;
		}

		#region IEquatable<Location3D> Members

		public bool Equals(IntPoint3D other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint3D))
				return false;

			IntPoint3D l = (IntPoint3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(IntPoint3D left, IntPoint3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntPoint3D left, IntPoint3D right)
		{
			return !(left == right);
		}

		public static IntPoint3D operator +(IntPoint3D left, IntPoint3D right)
		{
			return new IntPoint3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntPoint3D operator -(IntPoint3D left, IntPoint3D right)
		{
			return new IntPoint3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public override int GetHashCode()
		{
			return (this.X ^ this.Y ^ this.Z);
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"IntPoint3D({0}, {1}, {2})", X, Y, Z);
		}
	}
}
