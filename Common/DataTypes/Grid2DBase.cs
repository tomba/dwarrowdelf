using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class Grid2DBase<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Grid2DBase(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			this.Grid = new T[width * height];
		}

		protected T[] Grid { get; private set; }

		protected int GetIndex(IntPoint p)
		{
			return p.Y * this.Width + p.X;
		}
	}
}
