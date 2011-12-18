using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(IntSize3DConverter))]
	public struct IntSize3D : IEquatable<IntSize3D>
	{
		readonly int m_width;
		readonly int m_height;
		readonly int m_depth;

		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }
		public int Depth { get { return m_depth; } }

		public IntSize3D(int width, int height, int depth)
		{
			m_width = width;
			m_height = height;
			m_depth = depth;
		}

		public IntSize3D(IntSize size, int depth)
		{
			m_width = size.Width;
			m_height = size.Height;
			m_depth = depth;
		}

		public bool IsEmpty
		{
			get { return this.Width == 0 && this.Height == 0 && this.Depth == 0; }
		}

		#region IEquatable<IntSize3D> Members

		public bool Equals(IntSize3D s)
		{
			return ((s.Width == this.Width) && (s.Height == this.Height) && (s.Depth == this.Depth));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntSize3D))
				return false;

			IntSize3D s = (IntSize3D)obj;
			return ((s.Width == this.Width) && (s.Height == this.Height) && (s.Depth == this.Depth));
		}

		public static bool operator ==(IntSize3D left, IntSize3D right)
		{
			return ((left.Width == right.Width) && (left.Height == right.Height) && (left.Depth == right.Depth));
		}

		public static bool operator !=(IntSize3D left, IntSize3D right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return (this.Width ^ this.Height ^ this.Depth);
		}

		public override string ToString()
		{
			return String.Format("IntSize3D({0}, {1}, {2})", Width, Height, Depth);
		}

		internal string ConvertToString()
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			return String.Format(info, "{0},{1},{2}", m_width, m_height, m_depth);
		}

		public static IntSize3D Parse(string str)
		{
			var info = System.Globalization.NumberFormatInfo.InvariantInfo;
			var arr = str.Split(',');
			return new IntSize3D(Convert.ToInt32(arr[0], info), Convert.ToInt32(arr[1], info), Convert.ToInt32(arr[2], info));
		}
	}
}
