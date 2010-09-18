using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public class ShortGrid3DBase<T>
	{
		public short Width { get; private set; }
		public short Height { get; private set; }
		public short Depth { get; private set; }
		public short m_width;
		public short m_height;
		public short m_depth;

		protected ShortGrid3DBase(int width, int height, int depth)
		{
			this.Width = (short)width;
			this.Height = (short)height;
			this.Depth = (short)depth;
			this.Grid = new T[width, height, depth];
			m_width = (short)width;
			m_height = (short)height;
			m_depth = (short)depth;
		}

		public T[,,] Grid { get; private set; }

		public int GetIndex(short x, short y, short z)
		{
			return x + y * m_width + z * m_width * m_height;
		}

		public int GetIndex(ShortPoint3D p)
		{
			return p.X + p.Y * this.Width + p.Z * this.Width * this.Height;
		}

		public IntCuboid Bounds
		{
			get
			{
				return new IntCuboid(0, 0, 0, this.Width, this.Height, this.Depth);
			}
		}
	}
}
