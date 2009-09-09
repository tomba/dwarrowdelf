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

		public bool Equals(IntVector other)
		{
			return ((other.X == this.X) && (other.Y == this.Y));
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
			get { return Math.Sqrt(this.X * this.X + this.Y * this.Y); }
		}

		public int ManhattanLength
		{
			get { return Math.Abs(this.X) + Math.Abs(this.Y); }
		}

		public void Normalize()
		{
			double len = this.Length;
			this.X = (int)Math.Round(this.X / len);
			this.Y = (int)Math.Round(this.Y / len);
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

		public static IntVector operator *(IntVector left, int number)
		{
			return new IntVector(left.X * number, left.Y * number);
		}

		public override int GetHashCode()
		{
			return (this.X << 16) | this.Y;
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"IntVector({0}, {1})", X, Y);
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

			Direction dir = 0;

			if (v.X > 0)
				dir |= Direction.East;
			else if (v.X < 0)
				dir |= Direction.West;

			if (v.Y > 0)
				dir |= Direction.North;
			else if (v.Y < 0)
				dir |= Direction.South;

			return dir;
		}

		public static IntVector FromDirection(Direction dir)
		{
			int x, y;

			x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;
			if (y == DirectionConsts.DirNeg)
				y = -1;

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

		/// <summary>
		/// Return 8 IntVectors pointing to main directions on X-Y plane, each rotated 45 degrees
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<IntVector> GetAllXYDirections()
		{
			return GetAllXYDirections(Direction.North);
		}

		/// <summary>
		/// Return 8 IntVectors pointing to main directions on X-Y plane, each rotated 45 degrees
		/// </summary>
		/// <param name="startDir">Start direction</param>
		/// <returns></returns>
		public static IEnumerable<IntVector> GetAllXYDirections(Direction startDir)
		{
			var v = FromDirection(startDir);
			for (int i = 0; i < 8; ++i, v.FastRotate(1))
				yield return v;
		}




		static int FastCos(int rot)
		{
			rot %= 8;
			if (rot < 0)
				rot += 8;
			if (rot == 0 || rot == 1 || rot == 7)
				return 1;
			if (rot == 2 || rot == 6)
				return 0;
			return -1;
		}

		static int FastSin(int rot)
		{
			rot %= 8;
			if (rot < 0)
				rot += 8;
			if (rot == 1 || rot == 2 || rot == 3)
				return 1;
			if (rot == 0 || rot == 4)
				return 0;
			return -1;
		}

		static int FastMul(int a, int b)
		{
			if (a == 0 || b == 0)
				return 0;
			if (a == b)
				return 1;
			return -1;
		}

		/// <summary>
		/// Rotate unit vector in 45 degree steps
		/// </summary>
		/// <param name="rotate">Rotation units, in 45 degree steps</param>
		public void FastRotate(int rotate)
		{
			int x = FastMul(FastCos(rotate), this.X) - FastMul(FastSin(rotate), this.Y);
			int y = FastMul(FastSin(rotate), this.X) + FastMul(FastCos(rotate), this.Y);

			this.X = x > 1 ? 1 : (x < -1 ? -1 : x);
			this.Y = y > 1 ? 1 : (y < -1 ? -1 : y);
		}

		public static Direction RotateDir(Direction dir, int rotate)
		{
			int x = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;

			if (x == DirectionConsts.DirNeg)
				x = -1;

			if (y == DirectionConsts.DirNeg)
				y = -1;

			IntVector v = new IntVector(x, y);
			v.FastRotate(rotate);
			return v.ToDirection();
		}
	}

}
