using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	public struct IntVector3D : IEquatable<IntVector3D>
	{
		[DataMember]
		public int X { get; set; }
		[DataMember]
		public int Y { get; set; }
		[DataMember]
		public int Z { get; set; }

		public IntVector3D(int x, int y, int z)
			: this()
		{
			X = x;
			Y = y;
			Z = z;
		}

		#region IEquatable<IntVector3D> Members

		public bool Equals(IntVector3D other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntVector3D))
				return false;

			IntVector3D l = (IntVector3D)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public double Length
		{
			get { return Math.Pow(this.X * this.X + this.Y * this.Y + this.Z * this.Z, 1.0 / 3.0); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y) + Math.Abs(this.Z); }
		}

		public void Normalize()
		{
			double len = this.Length;
			this.X = (int)Math.Round(this.X / len);
			this.Y = (int)Math.Round(this.Y / len);
			this.Z = (int)Math.Round(this.Z / len);
		}

		public static bool operator ==(IntVector3D left, IntVector3D right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntVector3D left, IntVector3D right)
		{
			return !(left == right);
		}

		public static IntVector3D operator +(IntVector3D left, IntVector3D right)
		{
			return new IntVector3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);			
		}

		public static IntVector3D operator -(IntVector3D left, IntVector3D right)
		{
			return new IntVector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntVector3D operator *(IntVector3D left, int number)
		{
			return new IntVector3D(left.X * number, left.Y * number, left.Z * number);
		}

		public override int GetHashCode()
		{
			return (this.X << 20) | (this.Y << 10) | this.Z;
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"IntVector3D({0}, {1}, {2})", X, Y, Z);
		}

		public static explicit operator IntVector3D(IntPoint3D point)
		{
			return new IntVector3D(point.X, point.Y, point.Z);
		}
	}
}
