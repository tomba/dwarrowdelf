using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntGrid2ZConverter))]
	public struct IntGrid2Z : IEquatable<IntGrid2Z>
	{
		readonly IntGrid2 m_grid;
		readonly int m_z;

		public int X { get { return m_grid.X; } }
		public int Y { get { return m_grid.Y; } }
		public int Columns { get { return m_grid.Columns; } }
		public int Rows { get { return m_grid.Rows; } }
		public int Z { get { return m_z; } }

		public IntGrid2Z(int x, int y, int columns, int rows, int z)
		{
			m_grid = new IntGrid2(x, y, columns, rows);
			m_z = z;
		}

		public IntGrid2Z(IntPoint2 point1, IntPoint2 point2, int z)
		{
			m_grid = new IntGrid2(point1, point2);
			m_z = z;
		}

		public IntGrid2Z(IntPoint2 point, IntSize2 size, int z)
			: this(point.X, point.Y, size.Width, size.Height, z)
		{
		}

		public IntGrid2Z(IntGrid2 rect, int z)
		{
			m_grid = rect;
			m_z = z;
		}

		public int X1 { get { return X; } }
		public int X2 { get { return X + Columns - 1; } }
		public int Y1 { get { return Y; } }
		public int Y2 { get { return Y + Rows - 1; } }

		public IntPoint3 X1Y1
		{
			get { return new IntPoint3(m_grid.X1Y1, this.Z); }
		}

		public IntPoint3 X2Y2
		{
			get { return new IntPoint3(m_grid.X2Y2, this.Z); }
		}

		public IntPoint3 Center
		{
			get { return new IntPoint3(m_grid.Center, this.Z); }
		}

		public int Area
		{
			get { return m_grid.Area; }
		}

		public IntSize2 Size
		{
			get { return m_grid.Size; }
		}

		public bool IsNull
		{
			get { return m_grid.IsNull; }
		}

		public static bool operator ==(IntGrid2Z left, IntGrid2Z right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntGrid2Z left, IntGrid2Z right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntGrid2Z other)
		{
			return this.m_grid == other.m_grid && this.m_z == other.m_z;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is IntGrid2Z))
				return false;

			return Equals((IntGrid2Z)obj);
		}

		public bool Contains(IntPoint3 l)
		{
			return m_grid.Contains(l.ToIntPoint()) && l.Z == m_z;
		}

		public bool IntersectsWith(IntGrid2Z rect)
		{
			return rect.Z == this.Z && m_grid.IntersectsWith(rect.m_grid);
		}

		public IntGrid2Z Inflate(int columns, int rows)
		{
			return new IntGrid2Z(m_grid.Inflate(columns, rows), this.Z);
		}

		public IEnumerable<IntPoint3> Range()
		{
			for (int y = this.Y; y < this.Y + this.Rows; ++y)
				for (int x = this.X; x < this.X + this.Columns; ++x)
					yield return new IntPoint3(x, y, m_z);
		}

		public IntGrid3 ToIntGrid3()
		{
			return new IntGrid3(m_grid, m_z);
		}

		public override int GetHashCode()
		{
			return this.X | (this.Y << 8) | (this.Columns << 16) | (this.Rows << 24);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3},{4}", this.X, this.Y, this.Columns, this.Rows, this.Z);
		}

		public static IntGrid2Z Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntGrid2Z(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info), Convert.ToInt32(arr[3], info), Convert.ToInt32(arr[4], info));
		}
	}
}
