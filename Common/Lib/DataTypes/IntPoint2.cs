using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntPointConverter))]
	public struct IntPoint2 : IEquatable<IntPoint2>
	{
		readonly int m_x;
		readonly int m_y;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }

		public IntPoint2(int x, int y)
		{
			m_x = x;
			m_y = y;
		}

		#region IEquatable<IntPoint2> Members

		public bool Equals(IntPoint2 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint2))
				return false;

			IntPoint2 l = (IntPoint2)obj;
			return ((l.X == this.X) && (l.Y == this.Y));
		}

		public IntPoint2 Offset(int offsetX, int offsetY)
		{
			return new IntPoint2(this.X + offsetX, this.Y + offsetY);
		}

		public static bool operator ==(IntPoint2 left, IntPoint2 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator !=(IntPoint2 left, IntPoint2 right)
		{
			return !(left == right);
		}


		public static IntPoint2 operator +(IntPoint2 left, IntVector2 right)
		{
			return new IntPoint2(left.X + right.X, left.Y + right.Y);
		}

		public static IntVector2 operator -(IntPoint2 left, IntPoint2 right)
		{
			return new IntVector2(left.X - right.X, left.Y - right.Y);
		}

		public static IntPoint2 operator -(IntPoint2 left, IntVector2 right)
		{
			return new IntPoint2(left.X - right.X, left.Y - right.Y);
		}

		public static IntPoint2 operator *(IntPoint2 left, int right)
		{
			return new IntPoint2(left.X * right, left.Y * right);
		}

		public static IntPoint2 operator +(IntPoint2 left, Direction right)
		{
			return left + new IntVector2(right);
		}

		public override int GetHashCode()
		{
			return Hash.Hash2D(this.X, this.Y);
		}

		public static explicit operator IntPoint2(IntVector2 vector)
		{
			return new IntPoint2(vector.X, vector.Y);
		}

		/// <summary>
		/// Returns a square spiral, centered at center, covering an area of size * size
		/// </summary>
		public static IEnumerable<IntPoint2> SquareSpiral(IntPoint2 center, int size)
		{
			var p = center;
			var v = new IntVector2(1, 0);

			for (int loop = 0; loop < size * 2 - 1; ++loop)
			{
				for (int i = 0; i < loop / 2 + 1; ++i)
				{
					yield return p;
					p += v;
				}

				v = v.FastRotate(2);
			}
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1}", this.X, this.Y);
		}

		public static IntPoint2 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntPoint2(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info));
		}
	}
}
