using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	/**
	 * Location3D datatype, x/y/z
	 */
	[DataContract]
	public struct Location3D : IEquatable<Location3D>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }
		[DataMember]
		public int Z { get; set; }

		public Location3D(int x, int y, int z)
			: this()
		{
			X = x;
			Y = y;
			Z = z;
		}

		#region IEquatable<Location3D> Members

		public bool Equals(Location3D l)
		{
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is Location3D))
				return false;

			Location3D l = (Location3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(Location3D left, Location3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(Location3D left, Location3D right)
		{
			return !(left == right);
		}

		public static Location3D operator +(Location3D left, Location3D right)
		{
			return new Location3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static Location3D operator -(Location3D left, Location3D right)
		{
			return new Location3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public override int GetHashCode()
		{
			return (this.X ^ this.Y ^ this.Z);
		}

		public override string ToString()
		{
			return String.Format("Location3D({0}, {1}, {2})", X, Y, Z);
		}
	}
}
