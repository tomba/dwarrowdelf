using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntCuboidConverter))]
	public struct IntCuboid : IEquatable<IntCuboid>
	{
		readonly int m_x;
		readonly int m_y;
		readonly int m_z;
		readonly int m_width;
		readonly int m_height;
		readonly int m_depth;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Z { get { return m_z; } }
		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }
		public int Depth { get { return m_depth; } }

		public IntCuboid(int x, int y, int z, int width, int height, int depth)
		{
			m_x = x;
			m_y = y;
			m_z = z;
			m_width = width;
			m_height = height;
			m_depth = depth;
		}

		public IntCuboid(IntPoint3 p, IntSize3D size)
		{
			m_x = p.X;
			m_y = p.Y;
			m_z = p.Z;
			m_width = size.Width;
			m_height = size.Height;
			m_depth = size.Depth;
		}

		public IntCuboid(IntPoint3 point1, IntPoint3 point2)
		{
			m_x = Math.Min(point1.X, point2.X);
			m_y = Math.Min(point1.Y, point2.Y);
			m_z = Math.Min(point1.Z, point2.Z);
			m_width = Math.Max((Math.Max(point1.X, point2.X) - m_x), 0);
			m_height = Math.Max((Math.Max(point1.Y, point2.Y) - m_y), 0);
			m_depth = Math.Max((Math.Max(point1.Z, point2.Z) - m_z), 0);
		}

		public IntCuboid(IntRect rect, int z)
			: this(rect.X, rect.Y, z, rect.Width, rect.Height, 1)
		{
		}

		public IntCuboid(IntRectZ rect)
			: this(rect.X, rect.Y, rect.Z, rect.Width, rect.Height, 1)
		{
		}

		public IntCuboid(IntSize3D size)
			: this(0, 0, 0, size.Width, size.Height, size.Depth)
		{
		}

		public int X1 { get { return this.X; } }
		public int X2 { get { return this.X + this.Width; } }
		public int Y1 { get { return this.Y; } }
		public int Y2 { get { return this.Y + this.Height; } }
		public int Z1 { get { return this.Z; } }
		public int Z2 { get { return this.Z + this.Depth; } }

		public IntPoint3 Corner1
		{
			get { return new IntPoint3(this.X, this.Y, this.Z); }
		}

		public IntPoint3 Corner2
		{
			get { return new IntPoint3(this.X + this.Width, this.Y + this.Height, this.Z + this.Depth); }
		}

		public IntPoint3 Center
		{
			get { return new IntPoint3((this.X1 + this.X2) / 2, (this.Y1 + this.Y2) / 2, (this.Z1 + this.Z2) / 2); }
		}

		public int Volume
		{
			get { return this.Width * this.Height * this.Depth; }
		}

		public IntRect Plane
		{
			get { return new IntRect(this.X, this.Y, this.Width, this.Height); }
		}

		public bool IsNull
		{
			get { return this.Width == 0 && this.Height == 0 && this.Depth == 0; }
		}

		public int GetIndex(IntPoint3 p)
		{
			return p.X + p.Y * this.Width + p.Z * this.Width * this.Height;
		}

		public bool Contains(IntPoint3 l)
		{
			if (l.X < this.X || l.Y < this.Y || l.Z < this.Z ||
				l.X >= this.X + this.Width || l.Y >= this.Y + this.Height || l.Z >= this.Z + this.Depth)
				return false;
			else
				return true;
		}

		public bool ContainsZ(int z)
		{
			if (z < this.Z1 || z >= this.Z2)
				return false;
			else
				return true;
		}

		public bool IntersectsWith(IntCuboid rect)
		{
			return rect.X1 < this.X2 && rect.X2 > this.X1 &&
				rect.Y1 < this.Y2 && rect.Y2 > this.Y1 &&
				rect.Z1 < this.Z1 && rect.Z2 > this.Z1;
		}

		public bool IntersectsWithInclusive(IntCuboid rect)
		{
			return rect.X1 <= this.X2 && rect.X2 >= this.X1 &&
				rect.Y1 <= this.Y2 && rect.Y2 >= this.Y1 &&
				rect.Z1 <= this.Z1 && rect.Z2 >= this.Z1;
		}

		public IntCuboid Inflate(int width, int height, int depth)
		{
			return new IntCuboid(this.X, this.Y, this.Z, this.Width + width, this.Height + height, this.Depth + depth);
		}

		public IEnumerable<IntPoint3> Range()
		{
			for (int z = this.Z; z < this.Z + this.Depth; ++z)
				for (int y = this.Y; y < this.Y + this.Height; ++y)
					for (int x = this.X; x < this.X + this.Width; ++x)
						yield return new IntPoint3(x, y, z);
		}

		public IntRect ToIntRect()
		{
			return new IntRect(this.X, this.Y, this.Width, this.Height);
		}

		public static bool operator ==(IntCuboid left, IntCuboid right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntCuboid left, IntCuboid right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntCuboid other)
		{
			return this.m_x == other.m_x && this.m_y == other.m_y && this.m_z == other.m_x &&
				this.m_width == other.m_width && this.m_height == other.m_height && this.m_width == other.m_width;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is IntCuboid))
				return false;

			return Equals((IntCuboid)obj);
		}

		public override int GetHashCode()
		{
			return ((this.Width ^ this.Height ^ this.Depth) << 16) | (this.X ^ this.Y ^ this.Z);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3},{4},{5}", m_x, m_y, m_z, m_width, m_height, m_depth);
		}

		public static IntCuboid Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntCuboid(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info),
				Convert.ToInt32(arr[3], info), Convert.ToInt32(arr[4], info), Convert.ToInt32(arr[5], info));
		}
	}
}
