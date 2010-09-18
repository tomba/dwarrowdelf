using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public class ArrayGrid2D<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public IntSize Size { get { return new IntSize(this.Width, this.Height); } }

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

		public ArrayGrid2D(IntSize size) : this(size.Width, size.Height)
		{
		}

		public IntRect Bounds { get { return new IntRect(0, 0, this.Width, this.Height); } }

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Grid.Length);
		}
	}
}
