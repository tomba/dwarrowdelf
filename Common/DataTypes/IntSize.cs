using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyGame
{
	[DataContract]
	[Serializable]
	public struct IntSize : IEquatable<IntSize>
	{
		[DataMember(Name = "Width")]
		readonly int m_width;
		[DataMember(Name = "Height")]
		readonly int m_height;

		public int Width { get { return m_width; } }
		public int Height { get { return m_height; } }

		public IntSize(int width, int height)
		{
			m_width = width;
			m_height = height;
		}

		public bool IsEmpty
		{
			get
			{
				return this.Width == 0 && this.Height == 0;
			}
		}

		#region IEquatable<IntSize> Members

		public bool Equals(IntSize s)
		{
			return ((s.Width == this.Width) && (s.Height == this.Height));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is IntSize))
				return false;

			IntSize s = (IntSize)obj;
			return ((s.Width == this.Width) && (s.Height == this.Height));
		}

		public static bool operator ==(IntSize left, IntSize right)
		{
			return ((left.Width == right.Width) && (left.Height == right.Height));
		}

		public static bool operator !=(IntSize left, IntSize right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return (this.Width ^ this.Height);
		}

		public override string ToString()
		{
			return String.Format("IntSize({0}, {1})", Width, Height);
		}


		public static explicit operator IntVector(IntSize size)
		{
			return new IntVector(size.Width, size.Height);
		}
	}

}
