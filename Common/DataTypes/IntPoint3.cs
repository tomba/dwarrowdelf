using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntPoint3DConverter))]
	public struct IntPoint3 : IEquatable<IntPoint3>
	{
		// Note: this could be optimized by encoding all values into one int

		readonly int m_x;
		readonly int m_y;
		readonly int m_z;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }

		public IntPoint3(int x, int y, int z)
		{
			m_x = x;
			m_y = y;
			m_z = z;
		}

		public IntPoint3(IntPoint2 p, int z)
		{
			m_x = p.X;
			m_y = p.Y;
			m_z = z;
		}

		#region IEquatable<IntPoint3> Members

		public bool Equals(IntPoint3 other)
		{
			return ((other.X == this.X) && (other.Y == this.Y) && (other.Z == this.Z));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntPoint3))
				return false;

			IntPoint3 l = (IntPoint3)obj;
			return ((l.X == this.X) && (l.Y == this.Y) && (l.Z == this.Z));
		}

		public static bool operator ==(IntPoint3 left, IntPoint3 right)
		{
			return ((left.X == right.X) && (left.Y == right.Y) && (left.Z == right.Z));
		}

		public static bool operator !=(IntPoint3 left, IntPoint3 right)
		{
			return !(left == right);
		}

		public static IntPoint3 operator +(IntPoint3 left, IntVector3 right)
		{
			return new IntPoint3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
		}

		public static IntPoint3 operator -(IntPoint3 left, IntVector3 right)
		{
			return new IntPoint3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3 operator +(IntPoint3 left, Direction right)
		{
			return left + new IntVector3(right);
		}

		public static IntVector3 operator -(IntPoint3 left, IntPoint3 right)
		{
			return new IntVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
		}

		public static IntPoint3 operator +(IntPoint3 left, IntVector2 right)
		{
			return new IntPoint3(left.X + right.X, left.Y + right.Y, left.Z);
		}

		public bool IsAdjacentTo(IntPoint3 p, DirectionSet positioning)
		{
			var v = p - this;

			if (!v.IsNormal)
				return false;

			var d = v.ToDirection();

			return positioning.Contains(d);
		}

		public override int GetHashCode()
		{
			return Hash.Hash3D(this.X, this.Y, this.Z);
		}

		public static IEnumerable<IntPoint3> Range(int start_x, int start_y, int start_z, int width, int height, int depth)
		{
			int max_x = start_x + width;
			int max_y = start_y + height;
			int max_z = start_z + depth;

			for (int z = start_z; z < max_z; ++z)
				for (int y = start_y; y < max_y; ++y)
					for (int x = start_x; x < max_x; ++x)
						yield return new IntPoint3(x, y, z);
		}

		public static IEnumerable<IntPoint3> Range(int width, int height, int depth)
		{
			for (int z = 0; z < depth; ++z)
				for (int y = 0; y < height; ++y)
					for (int x = 0; x < width; ++x)
						yield return new IntPoint3(x, y, z);
		}

		public IntPoint2 ToIntPoint()
		{
			return new IntPoint2(this.X, this.Y);
		}

		public static IntPoint3 Center(IEnumerable<IntPoint3> points)
		{
			int x, y, z;
			int count = 0;
			x = y = z = 0;

			foreach (var p in points)
			{
				x += p.X;
				y += p.Y;
				z += p.Z;
				count++;
			}

			return new IntPoint3(x / count, y / count, z / count);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", this.X, this.Y, this.Z);
		}

		public static IntPoint3 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntPoint3(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
		}
	}
}
