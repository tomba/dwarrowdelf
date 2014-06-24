using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntGrid2Converter))]
	public struct IntGrid2 : IEquatable<IntGrid2>
	{
		readonly int m_x;
		readonly int m_y;
		readonly int m_columns;
		readonly int m_rows;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		public IntGrid2(int x, int y, int columns, int rows)
		{
			m_x = x;
			m_y = y;
			m_columns = columns;
			m_rows = rows;
		}

		public IntGrid2(IntVector2 point1, IntVector2 point2)
		{
			m_x = Math.Min(point1.X, point2.X);
			m_y = Math.Min(point1.Y, point2.Y);
			m_columns = Math.Max(point1.X, point2.X) - m_x + 1;
			m_rows = Math.Max(point1.Y, point2.Y) - m_y + 1;
		}

		public IntGrid2(IntVector2 point, IntSize2 size)
			: this(point.X, point.Y, size.Width, size.Height)
		{
		}

		public int X1 { get { return X; } }
		public int X2 { get { return X + Columns - 1; } }
		public int Y1 { get { return Y; } }
		public int Y2 { get { return Y + Rows - 1; } }

		public IntVector2 X1Y1
		{
			get { return new IntVector2(this.X, this.Y); }
		}

		public IntVector2 X2Y2
		{
			get { return new IntVector2(this.X + this.Columns - 1, this.Y + this.Rows - 1); }
		}

		public IntVector2 Center
		{
			get { return new IntVector2(this.X + (this.Columns - 1) / 2, this.Y + (this.Rows - 1) / 2); }
		}

		public int Area
		{
			get { return this.Columns * this.Rows; }
		}

		public IntSize2 Size
		{
			get { return new IntSize2(this.Columns, this.Rows); }
		}

		public bool IsNull
		{
			get { return this.Columns == 0 && this.Rows == 0; }
		}

		public static bool operator ==(IntGrid2 left, IntGrid2 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntGrid2 left, IntGrid2 right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntGrid2 other)
		{
			return this.m_x == other.m_x && this.m_y == other.m_y && this.m_columns == other.m_columns && this.m_rows == other.m_rows;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is IntGrid2))
				return false;

			return Equals((IntGrid2)obj);
		}

		public IntGrid2 Move(int x, int y)
		{
			return new IntGrid2(x, y, this.Columns, this.Rows);
		}

		public IntGrid2 Resize(int columns, int rows)
		{
			return new IntGrid2(this.X, this.Y, columns, rows);
		}

		public IntGrid2 Offset(int x, int y)
		{
			return new IntGrid2(this.X + x, this.Y + y, this.Columns, this.Rows);
		}

		public IntGrid2 Inflate(int columns, int rows)
		{
			return new IntGrid2(this.X, this.Y, this.Columns + columns, this.Rows + rows);
		}

		public bool Contains(IntVector2 p)
		{
			return (p.X >= this.X) && (p.X < this.X + this.Columns) && (p.Y >= this.Y) && (p.Y < this.Y + this.Rows);
		}

		public bool IntersectsWith(IntGrid2 grid)
		{
			return grid.X1 <= this.X2 && grid.X2 >= this.X1 && grid.Y1 <= this.Y2 && grid.Y2 >= this.Y1;
		}

		public IntGrid2 Intersect(IntGrid2 other)
		{
			int x1 = Math.Max(this.X, other.X);
			int x2 = Math.Min(this.X + this.Columns, other.X + other.Columns);
			int y1 = Math.Max(this.Y, other.Y);
			int y2 = Math.Min(this.Y + this.Rows, other.Y + other.Rows);

			if (x2 > x1 && y2 > y1)
				return new IntGrid2(x1, y1, x2 - x1, y2 - y1);

			return new IntGrid2();
		}

		public IntGrid2 Union(IntGrid2 other)
		{
			int x1 = Math.Min(this.X, other.X);
			int x2 = Math.Max(this.X + this.Columns, other.X + other.Columns);
			int y1 = Math.Min(this.Y, other.Y);
			int y2 = Math.Max(this.Y + this.Rows, other.Y + other.Rows);

			return new IntGrid2(x1, y1, x2 - x1, y2 - y1);
		}

		public IEnumerable<IntVector2> Range()
		{
			for (int y = this.Y; y < this.Y + this.Rows; ++y)
				for (int x = this.X; x < this.X + this.Columns; ++x)
					yield return new IntVector2(x, y);
		}

		public override int GetHashCode()
		{
			return this.X | (this.Y << 8) | (this.Columns << 16) | (this.Rows << 24);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3}", m_x, m_y, m_columns, m_rows);
		}

		public static IntGrid2 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntGrid2(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info), Convert.ToInt32(arr[3], info));
		}
	}
}
