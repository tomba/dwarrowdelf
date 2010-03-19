using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public struct IntRect : IEquatable<IntRect>
	{
		[DataMember(Name = "X")]
		readonly int m_x;
		[DataMember(Name = "Y")]
		readonly int m_y;
		[DataMember(Name = "Width")]
		readonly int m_width;
		[DataMember(Name = "Height")]
		readonly int m_height;

		public int X { get { return m_x; } }
		public int Y { get { return m_y; } }
		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }

		public IntRect(int x, int y, int width, int height)
		{
			m_x = x;
			m_y = y;
			m_width = width;
			m_height = height;
		}

		public IntRect(IntPoint point1, IntPoint point2)
		{
			m_x = Math.Min(point1.X, point2.X);
			m_y = Math.Min(point1.Y, point2.Y);
			m_width = Math.Max((Math.Max(point1.X, point2.X) - m_x), 0);
			m_height = Math.Max((Math.Max(point1.Y, point2.Y) - m_y), 0);
		}

		public IntRect(IntPoint point, IntSize size)
			: this(point.X, point.Y, size.Width, size.Height)
		{
		}

		public int X1 { get { return X; } }
		public int X2 { get { return X + Width; } }
		public int Y1 { get { return Y; } }
		public int Y2 { get { return Y + Height; } }

		public IntPoint X1Y1
		{
			get { return new IntPoint(this.X, this.Y); }
		}

		public IntPoint X2Y2
		{
			get { return new IntPoint(this.X + this.Width, this.Y + this.Height); }
		}

		public int Area
		{
			get { return this.Width * this.Height; }
		}

		public IntSize Size
		{
			get { return new IntSize(this.Width, this.Height); }
		}

		public bool IsNull
		{
			get { return this.Width == 0 && this.Height == 0; }
		}

		public static bool operator ==(IntRect left, IntRect right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(IntRect left, IntRect right)
		{
			return !left.Equals(right);
		}

		public bool Equals(IntRect other)
		{
			return this.m_x == other.m_x && this.m_y == other.m_y && this.m_width == other.m_width && this.m_height == other.m_height;
		}

		public int GetIndex(IntPoint p)
		{
			return p.X + p.Y * this.Width;
		}

		public IntRect Move(int x, int y)
		{
			return new IntRect(x, y, this.Width, this.Height);
		}

		public IntRect Resize(int width, int height)
		{
			return new IntRect(this.X, this.Y, width, height);
		}

		public IntRect Offset(int x, int y)
		{
			return new IntRect(this.X + x, this.Y + y, this.Width, this.Height);
		}

		public IntRect Inflate(int width, int height)
		{
			return new IntRect(this.X, this.Y, this.Width + width, this.Height + height);
		}

		public bool Contains(IntPoint l)
		{
			if (l.X < this.X || l.Y < this.Y || l.X >= this.X + this.Width || l.Y >= this.Y + this.Height)
				return false;
			else
				return true;
		}

		public bool IntersectsWith(IntRect rect)
		{
			return rect.X1 < this.X2 && rect.X2 > this.X1 && rect.Y1 < this.Y2 && rect.Y2 > this.Y1;
		}

		public bool IntersectsWithInclusive(IntRect rect)
		{
			return rect.X1 <= this.X2 && rect.X2 >= this.X1 && rect.Y1 <= this.Y2 && rect.Y2 >= this.Y1;
		}

		public IEnumerable<IntPoint> Range()
		{
			for (int y = this.Y; y < this.Y + this.Height; ++y)
				for (int x = this.X; x < this.X + this.Width; ++x)
					yield return new IntPoint(x, y);
		}

		public override string ToString()
		{
			return ("{X=" + this.X + ",Y=" + this.Y + ",Width=" + this.Width + ",Height=" + this.Height + "}");
		}
	}
}
