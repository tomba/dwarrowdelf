using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntPointConverter))]
	public struct IntPoint : IEquatable<IntPoint>
	{
		readonly int m_x;
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

		public static IntPoint operator +(IntPoint left, Direction right)
		{
			return left + new IntVector(right);
		}

		public override int GetHashCode()
		{
			return (this.Y << 16) | this.X;
		}

		public static explicit operator IntPoint(IntVector vector)
		{
			return new IntPoint(vector.X, vector.Y);
		}

		public static IEnumerable<IntPoint> Range(int width, int height)
		{
			for (int y = 0; y < height; ++y)
				for (int x = 0; x < width; ++x)
					yield return new IntPoint(x, y);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1}", this.X, this.Y);
		}

		public static IntPoint Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntPoint(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info));
		}
	}
}
