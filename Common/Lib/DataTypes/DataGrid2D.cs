using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public sealed class DataGrid2D<T>
	{
		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public IntSize2 Size { get; private set; }
		public bool Invalid { get; set; }

		public T[] Grid { get; private set; }

		public DataGrid2D()
		{
			this.Grid = new T[0];
		}

		public void SetMaxSize(IntSize2 size)
		{
			int len = size.Width * size.Height;

			if (len == this.Grid.Length)
				return;

			this.Grid = new T[size.Width * size.Height];
			this.Invalid = true;
		}

		public void SetSize(IntSize2 size)
		{
			if (this.Size == size)
				return;

			int len = size.Width * size.Height;
			if (len > this.Grid.Length)
				throw new Exception();

			this.Size = size;
			this.Invalid = true;
		}

		public int GetIdx(int x, int y)
		{
			return x + y * this.Size.Width;
		}

		public int GetIdx(IntPoint2 p)
		{
			return p.X + p.Y * this.Size.Width;
		}

		public bool Contains(IntPoint2 p)
		{
			return p.X >= 0 && p.X < this.Width && p.Y >= 0 && p.Y < this.Height;
		}

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Size.Width * this.Size.Height);
		}

		public void Scroll(IntVector2 scrollVector)
		{
			//Debug.WriteLine("RenderView.ScrollTiles");

			var columns = this.Width;
			var rows = this.Height;
			var grid = this.Grid;

			var ax = Math.Abs(scrollVector.X);
			var ay = Math.Abs(scrollVector.Y);

			if (ax >= columns || ay >= rows)
			{
				this.Invalid = true;
				return;
			}

			int srcIdx = 0;
			int dstIdx = 0;

			if (scrollVector.X >= 0)
				srcIdx += ax;
			else
				dstIdx += ax;

			if (scrollVector.Y >= 0)
				srcIdx += columns * ay;
			else
				dstIdx += columns * ay;

			var xClrIdx = scrollVector.X >= 0 ? columns - ax : 0;
			var yClrIdx = scrollVector.Y >= 0 ? rows - ay : 0;

			Array.Copy(grid, srcIdx, grid, dstIdx, columns * rows - ax - columns * ay);

			for (int y = 0; y < rows; ++y)
				Array.Clear(grid, y * columns + xClrIdx, ax);

			Array.Clear(grid, yClrIdx * columns, columns * ay);
		}
	}
}
