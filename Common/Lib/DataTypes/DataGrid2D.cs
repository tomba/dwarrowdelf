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

			var oldSize = this.Size;

			int len = size.Width * size.Height;
			if (len > this.Grid.Length)
				throw new Exception();

			this.Size = size;

			Scale(oldSize, size);
		}

		public int GetIdx(int x, int y)
		{
			return x + y * this.Size.Width;
		}

		public int GetIdx(IntVector2 p)
		{
			return p.X + p.Y * this.Size.Width;
		}

		public bool Contains(IntVector2 p)
		{
			return p.X >= 0 && p.X < this.Width && p.Y >= 0 && p.Y < this.Height;
		}

		public void Clear()
		{
			Array.Clear(this.Grid, 0, this.Size.Width * this.Size.Height);
		}

		void Scale(IntSize2 oldSize, IntSize2 newSize)
		{
			if (newSize.Width <= oldSize.Width && newSize.Height <= oldSize.Height)
			{
				var xdiff = oldSize.Width - newSize.Width;
				var ydiff = oldSize.Height - newSize.Height;

				xdiff /= 2;
				ydiff /= 2;

				var grid = this.Grid;

				for (int y = 0; y < newSize.Height; ++y)
				{
					Array.Copy(grid, (y + ydiff) * oldSize.Width + xdiff,
						grid, y * newSize.Width + 0, newSize.Width);
				}
			}
			else if (newSize.Width >= oldSize.Width && newSize.Height >= oldSize.Height)
			{
				var xdiff = oldSize.Width - newSize.Width;
				var ydiff = oldSize.Height - newSize.Height;

				xdiff /= -2;
				ydiff /= -2;

				var grid = this.Grid;

				for (int y = oldSize.Height - 1; y >= 0; --y)
				{
					Array.Copy(grid, y * oldSize.Width + 0,
						grid, (y + ydiff) * newSize.Width + xdiff, oldSize.Width);
				}

				// clear top rows
				Array.Clear(grid, 0, ydiff * newSize.Width);

				// clear bottom rows
				Array.Clear(grid, (newSize.Height - ydiff) * newSize.Width, ydiff * newSize.Width);

				// clear the edges
				for (int y = ydiff; y < newSize.Height - ydiff; ++y)
				{
					Array.Clear(grid, y * newSize.Width, xdiff);
					Array.Clear(grid, y * newSize.Width + newSize.Width - xdiff, xdiff);
				}
			}
			else
			{
				this.Invalid = true;
			}
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
