using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	/**
	 * Location datatype, x/y
	 */
	[DataContract]
	public struct Location : IEquatable<Location>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }

		public Location(int x, int y)
			: this()
		{
			X = x;
			Y = y;
		}

		#region IEquatable<Location> Members

		public bool Equals(Location l)
		{
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is Location))
				return false;

			Location l = (Location)obj;
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		public static bool operator ==(Location left, Location right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator !=(Location left, Location right)
		{
			return !(left == right);
		}

		public static Location operator +(Location left, Location right)
		{
			return new Location(left.X + right.X, left.Y + right.Y);			
		}

		public static Location operator -(Location left, Location right)
		{
			return new Location(left.X - right.X, left.Y - right.Y);
		}

		public override int GetHashCode()
		{
			return (this.X ^ this.Y);
		}

		public override string ToString()
		{
			return String.Format("Location({0}, {1})", X, Y);
		}

	}

}
