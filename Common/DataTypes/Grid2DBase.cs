using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public abstract class Grid2DBase<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		protected T[,] Grid { get; private set; }

		protected Grid2DBase(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			this.Grid = new T[height, width];
		}
	}
}
