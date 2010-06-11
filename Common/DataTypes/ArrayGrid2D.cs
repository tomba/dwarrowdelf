using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class ArrayGrid2D<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		/// <summary>
		/// Grid[y, x]
		/// </summary>
		public T[,] Grid { get; private set; }

		public ArrayGrid2D(int width, int height)
		{
			this.Width = width;
			this.Height = height;
			this.Grid = new T[height, width];
		}

		public IntRect Bounds { get { return new IntRect(0, 0, this.Width, this.Height); } }
	}
}
