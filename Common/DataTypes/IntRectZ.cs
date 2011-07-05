using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntRectZConverter))]
	public struct IntRectZ : IEquatable<IntRectZ>
	{
		readonly IntRect m_rect;
		readonly int m_z;

		public int X { get { return m_rect.X; } }
		public int Y { get { return m_rect.Y; } }
		public int Width { get { return m_rect.Width; } }
		public int Height { get { return m_rect.Height; } }
		public int Z { get { return m_z; } }

		public IntRectZ(int x, int y, int width, int height, int z)
		{
			m_rect = new IntRect(x, y, width, height);
			m_z = z;
		}

		public IntRectZ(IntPoint point1, IntPoint point2, int z)
		{
			m_rect = new IntRect(point1, point2);
			m_z = z;
		}

		public IntRectZ(IntPoint point, IntSize size, int z)
			: this(point.X, point.Y, size.Width, size.Height, z)
		{
		}

		public IntRectZ(IntRect rect, int z)
		{
			m_rect = rect;
			m_z = z;
		}

		public IntPoint3D X1Y1
		{
			get { return new IntPoint3D(m_rect.X1Y1, this.Z); }
		}

		public IntPoint3D X2Y2
		{
			get { return new IntPoint3D(m_rect.X2Y2, this.Z); }
		}

		public IntPoint3D Center
		{
			get { return new IntPoint3D(m_rect.Center, this.Z); }
		}

		public int Area
		{
			get { return m_rect.Area; }
		}

		public IntSize Size
		{
			get { return m_rect.Size; }
		}

		public bool IsNull
		{
			get { return m_rect.IsNull; }
		}

		public static bool operator ==(IntRectZ left, IntRectZ right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntRectZ left, IntRectZ right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntRectZ other)
		{
			return this.m_rect == other.m_rect && this.m_z == other.m_z;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is IntRectZ))
				return false;

			return Equals((IntRectZ)obj);
		}

		public bool Contains(IntPoint3D l)
		{
			return m_rect.Contains(l.ToIntPoint()) && l.Z == m_z;
		}

		public bool IntersectsWith(IntRectZ rect)
		{
			return rect.Z == this.Z && m_rect.IntersectsWith(rect.m_rect);
		}

		public bool IntersectsWithInclusive(IntRectZ rect)
		{
			return rect.Z == this.Z && m_rect.IntersectsWithInclusive(rect.m_rect);
		}

		public IEnumerable<IntPoint3D> Range()
		{
			for (int y = this.Y; y < this.Y + this.Height; ++y)
				for (int x = this.X; x < this.X + this.Width; ++x)
					yield return new IntPoint3D(x, y, m_z);
		}

		public IntCuboid ToCuboid()
		{
			return new IntCuboid(m_rect, m_z);
		}

		public override int GetHashCode()
		{
			return this.X | (this.Y << 8) | (this.Width << 16) | (this.Height << 24);
		}

		public override string ToString()
		{
			return ("{X=" + this.X + ",Y=" + this.Y + ",Width=" + this.Width + ",Height=" + this.Height + ",Z=" + this.Z + "}");
		}

		internal string ConvertToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2},{3},{4}", this.X, this.Y, this.Width, this.Height, this.Z);
		}

		public static IntRectZ Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntRectZ(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info), Convert.ToInt32(arr[3], info), Convert.ToInt32(arr[4], info));
		}
	}
}
