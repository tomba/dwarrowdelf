using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	public struct IntSize2 : IEquatable<IntSize2>
	{
		readonly int m_width;
		readonly int m_height;

		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }

		public IntSize2(int width, int height)
		{
			m_width = width;
			m_height = height;
		}

		public bool IsEmpty
		{
			get { return this.Width == 0 && this.Height == 0; }
		}

		public bool Contains(IntPoint2 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.X < this.Width && p.Y < this.Height;
		}

		public IntPoint2 Center
		{
			get { return new IntPoint2((this.Width - 1) / 2, (this.Height - 1) / 2); }
		}

		#region IEquatable<IntSize2> Members

		public bool Equals(IntSize2 s)
		{
			return ((s.Width == this.Width) && (s.Height == this.Height));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntSize2))
				return false;

			IntSize2 s = (IntSize2)obj;
			return ((s.Width == this.Width) && (s.Height == this.Height));
		}

		public static bool operator ==(IntSize2 left, IntSize2 right)
		{
			return ((left.Width == right.Width) && (left.Height == right.Height));
		}

		public static bool operator !=(IntSize2 left, IntSize2 right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return Hash.Hash2D(this.Width, this.Height);
		}

		public override string ToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1}", this.Width, this.Height);
		}

		public static explicit operator IntVector2(IntSize2 size)
		{
			return new IntVector2(size.Width, size.Height);
		}
	}
}
