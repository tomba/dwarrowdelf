using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class Grid3DBase<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		protected Grid3DBase(int width, int height, int depth)
		{
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
			this.Grid = new T[width * height * depth];
		}

		protected T[] Grid { get; private set; }

		protected int GetIndex(IntPoint3D p)
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
