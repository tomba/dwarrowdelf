using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntGrid3Converter))]
	public struct IntGrid3 : IEquatable<IntGrid3>
	{
		readonly int m_x;
		readonly int m_y;
		readonly int m_z;
		readonly int m_columns;
		readonly int m_rows;
		readonly int m_depth;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }
		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }
		public int Depth { get { return m_depth; } }

		public IntGrid3(int x, int y, int z, int columns, int rows, int depth)
		{
			m_x = x;
			m_y = y;
			m_z = z;
			m_columns = columns;
			m_rows = rows;
			m_depth = depth;
		}

		public IntGrid3(IntVector3 p, IntSize3 size)
		{
			m_x = p.X;
			m_y = p.Y;
			m_z = p.Z;
			m_columns = size.Width;
			m_rows = size.Height;
			m_depth = size.Depth;
		}

		public IntGrid3(IntVector3 point1, IntVector3 point2)
		{
			m_x = Math.Min(point1.X, point2.X);
			m_y = Math.Min(point1.Y, point2.Y);
			m_z = Math.Min(point1.Z, point2.Z);
			m_columns = Math.Max(point1.X, point2.X) - m_x + 1;
			m_rows = Math.Max(point1.Y, point2.Y) - m_y + 1;
			m_depth = Math.Max(point1.Z, point2.Z) - m_z + 1;
		}

		public IntGrid3(IntGrid2 rect, int z)
			: this(rect.X, rect.Y, z, rect.Columns, rect.Rows, 1)
		{
		}

		public IntGrid3(IntGrid2Z rect)
			: this(rect.X, rect.Y, rect.Z, rect.Columns, rect.Rows, 1)
		{
		}

		public IntGrid3(IntSize3 size)
			: this(0, 0, 0, size.Width, size.Height, size.Depth)
		{
		}

		public int X1 { get { return this.X; } }
		public int X2 { get { return this.X + this.Columns - 1; } }
		public int Y1 { get { return this.Y; } }
		public int Y2 { get { return this.Y + this.Rows - 1; } }
		public int Z1 { get { return this.Z; } }
		public int Z2 { get { return this.Z + this.Depth - 1; } }

		public IntVector3 Corner1
		{
			get { return new IntVector3(this.X, this.Y, this.Z); }
		}

		public IntVector3 Corner2
		{
			get { return new IntVector3(this.X + this.Columns - 1, this.Y + this.Rows - 1, this.Z + this.Depth - 1); }
		}

		public IntSize3 Size
		{
			get { return new IntSize3(m_columns, m_rows, m_depth); }
		}

		public IntVector3 Center
		{
			get { return new IntVector3(this.X + (this.Columns - 1) / 2, this.Y + (this.Rows - 1) / 2, this.Z + (this.Depth - 1) / 2); }
		}

		public int Volume
		{
			get { return this.Columns * this.Rows * this.Depth; }
		}

		public IntGrid2 Plane
		{
			get { return new IntGrid2(this.X, this.Y, this.Columns, this.Rows); }
		}

		public bool IsNull
		{
			get { return this.Columns == 0 && this.Rows == 0 && this.Depth == 0; }
		}

		public bool Contains(IntVector3 p)
		{
			return (p.X >= this.X) && (p.X < this.X + this.Columns) && (p.Y >= this.Y) && (p.Y < this.Y + this.Rows) && (p.Z >= this.Z) && (p.Z < this.Z + this.Depth);
		}

		public bool ContainsZ(int z)
		{
			return z >= this.Z && z < this.Z + Depth;
		}

		public Containment ContainsInclusive(ref IntGrid3 other)
		{
			if (this.X2 < other.X1 || this.X1 > other.X2)
				return Containment.Disjoint;

			if (this.Y2 < other.Y1 || this.Y1 > other.Y2)
				return Containment.Disjoint;

			if (this.Z2 < other.Z1 || this.Z1 > other.Z2)
				return Containment.Disjoint;

			if (this.X1 <= other.X1 && other.X2 <= this.X2 &&
				this.Y1 <= other.Y1 && other.Y2 <= this.Y2 &&
				this.Z1 <= other.Z1 && other.Z2 <= this.Z2)
			{
				return Containment.Contains;
			}

			return Containment.Intersects;
		}

		public Containment ContainsExclusive(ref IntGrid3 other)
		{
			if (this.X2 < other.X1 || this.X1 > other.X2)
				return Containment.Disjoint;

			if (this.Y2 < other.Y1 || this.Y1 > other.Y2)
				return Containment.Disjoint;

			if (this.Z2 < other.Z1 || this.Z1 > other.Z2)
				return Containment.Disjoint;

			if (this.X1 < other.X1 && other.X2 < this.X2 &&
				this.Y1 < other.Y1 && other.Y2 < this.Y2 &&
				this.Z1 < other.Z1 && other.Z2 < this.Z2)
			{
				return Containment.Contains;
			}

			return Containment.Intersects;
		}

		public bool IntersectsWith(IntGrid3 rect)
		{
			return rect.X1 <= this.X2 && rect.X2 >= this.X1 &&
				rect.Y1 <= this.Y2 && rect.Y2 >= this.Y1 &&
				rect.Z1 <= this.Z1 && rect.Z2 >= this.Z1;
		}

		public IntGrid3 Intersect(IntGrid3 other)
		{
			int x1 = Math.Max(this.X, other.X);
			int x2 = Math.Min(this.X + this.Columns, other.X + other.Columns);
			int y1 = Math.Max(this.Y, other.Y);
			int y2 = Math.Min(this.Y + this.Rows, other.Y + other.Rows);
			int z1 = Math.Max(this.Z, other.Z);
			int z2 = Math.Min(this.Z + this.Depth, other.Z + other.Depth);

			if (x2 > x1 && y2 > y1 && z2 > z1)
				return new IntGrid3(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);

			return new IntGrid3();
		}

		public IntGrid3 Union(IntGrid3 other)
		{
			int x1 = Math.Min(this.X, other.X);
			int x2 = Math.Max(this.X + this.Columns, other.X + other.Columns);
			int y1 = Math.Min(this.Y, other.Y);
			int y2 = Math.Max(this.Y + this.Rows, other.Y + other.Rows);
			int z1 = Math.Min(this.Z, other.Z);
			int z2 = Math.Max(this.Z + this.Depth, other.Z + other.Depth);

			return new IntGrid3(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);
		}

		public IntGrid3 Inflate(int columns, int rows, int depth)
		{
			return new IntGrid3(this.X, this.Y, this.Z, this.Columns + columns, this.Rows + rows, this.Depth + depth);
		}

		public IEnumerable<IntVector3> Range()
		{
			for (int z = this.Z; z < this.Z + this.Depth; ++z)
				for (int y = this.Y; y < this.Y + this.Rows; ++y)
					for (int x = this.X; x < this.X + this.Columns; ++x)
						yield return new IntVector3(x, y, z);
		}

		public IntGrid2 ToIntRect()
		{
			return new IntGrid2(this.X, this.Y, this.Columns, this.Rows);
		}

		public static bool operator ==(IntGrid3 left, IntGrid3 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntGrid3 left, IntGrid3 right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntGrid3 other)
		{
			return this.m_x == other.m_x && this.m_y == other.m_y && this.m_z == other.m_z &&
				this.m_columns == other.m_columns && this.m_rows == other.m_rows && this.m_depth == other.m_depth;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is IntGrid3))
				return false;

			return Equals((IntGrid3)obj);
		}

		public override int GetHashCode()
		{
			return ((this.Columns ^ this.Rows ^ this.Depth) << 16) | (this.X ^ this.Y ^ this.Z);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3},{4},{5}", m_x, m_y, m_z, m_columns, m_rows, m_depth);
		}

		public static IntGrid3 Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntGrid3(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info),
				Convert.ToInt32(arr[3], info), Convert.ToInt32(arr[4], info), Convert.ToInt32(arr[5], info));
		}
	}
}
