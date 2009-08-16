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

		public double Length
		{
			get
			{
				return Math.Sqrt(this.X * this.X + this.Y * this.Y);
			}
		}

		public void Normalize()
		{
			this.X = (int)Math.Round(this.X / this.Length);
			this.Y = (int)Math.Round(this.Y / this.Length);
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

		public Direction ToDirection()
		{
			IntVector v = this;
			v.Normalize();

			Direction dir;

			if (v == new IntVector(1, 1))
				dir = Direction.SouthEast;
			else if (v == new IntVector(-1, -1))
				dir = Direction.NorthWest;
			else if (v == new IntVector(1, -1))
				dir = Direction.NorthEast;
			else if (v == new IntVector(-1, 1))
				dir = Direction.SouthWest;
			else if (v == new IntVector(1, 0))
				dir = Direction.East;
			else if (v == new IntVector(-1, 0))
				dir = Direction.West;
			else if (v == new IntVector(0, 1))
				dir = Direction.South;
			else if (v == new IntVector(0, -1))
				dir = Direction.North;
			else
				dir = Direction.None;

			return dir;
		}

		public static IntVector FromDirection(Direction dir)
		{
			int x = 0, y = 0;

			switch (dir)
			{
				case Direction.North: y = -1; break;
				case Direction.South: y = 1; break;
				case Direction.West: x = -1; break;
				case Direction.East: x = 1; break;
				case Direction.NorthWest: x = -1; y = -1; break;
				case Direction.SouthWest: x = -1; y = 1; break;
				case Direction.NorthEast: x = 1; y = -1; break;
				case Direction.SouthEast: x = 1; y = 1; break;
			}

			return new IntVector(x, y);
		}

		public void Rotate(int angle)
		{
			double rad = Math.PI * angle / 180.0;
			double x = Math.Cos(rad) * this.X - Math.Sin(rad) * this.Y;
			double y = Math.Sin(rad) * this.X + Math.Cos(rad) * this.Y;

			this.X = (int)Math.Round(x);
			this.Y = (int)Math.Round(y);
		}
	}

}
