using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public sealed class ArrayGrid3D<T>
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }
		/// <summary>
		/// Grid[z, y, x]
		/// </summary>
		public T[, ,] Grid { get; private set; }

		public ArrayGrid3D(int width, int height, int depth)
		{
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
			this.Grid = new T[depth, height, width];
		}

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Grid.Length);
		}
	}
}
