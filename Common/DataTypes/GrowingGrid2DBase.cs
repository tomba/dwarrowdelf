using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	/**
	 * 2D grid, made of smaller blocks, allowing the grid to grow
	 */
	public abstract class GrowingGrid2DBase<T>
	{
		protected class Block : ArrayGrid2D<T>
		{
			public Block(int size) : base(size, size) { }
			public new T[,] Grid { get { return base.Grid; } }
		}

		sealed class MainGrid : ArrayGrid2D<Block>
		{
			public MainGrid(int width, int height) : base(width, height) { }
			public Block this[int x, int y]
			{
				get { return base.Grid[y, x]; }
				set { base.Grid[y, x] = value; }
			}
		}

		MainGrid m_grid;

		// m_mainRect tells the size of m_grid, but also the origin
		IntRect m_mainRect;
		int m_blockSize;

		protected GrowingGrid2DBase(int blockSize)
		{
			m_blockSize = blockSize;
		}

		public int Width
		{
			get { return m_mainRect.Width * m_blockSize; }
		}

		public int Height
		{
			get { return m_mainRect.Height * m_blockSize; }
		}

		public IntRect Bounds
		{
			get
			{
				return new IntRect(m_mainRect.X * m_blockSize, m_mainRect.Y * m_blockSize,
					m_mainRect.Width * m_blockSize, m_mainRect.Height * m_blockSize);
			}
		}

		protected Block GetBlock(ref IntPoint p, bool allowResize)
		{
			int x = p.X;
			int y = p.Y;

			var block = GetBlock(ref x, ref y, allowResize);

			p = new IntPoint(x, y);

			return block;
		}

		protected Block GetBlock(ref int x, ref int y, bool allowResize)
		{
			if (m_grid == null)
			{
				m_mainRect = new IntRect(x / m_blockSize, y / m_blockSize, 1, 1);
				m_grid = new MainGrid(1, 1);
			}

			int blockX = Math.DivRem(x, m_blockSize, out x);
			int blockY = Math.DivRem(y, m_blockSize, out y);

			if (x < 0)
			{
				blockX -= 1;
				x = m_blockSize + x;
			}

			if (y < 0)
			{
				blockY -= 1;
				y = m_blockSize + y;
			}

			if (!m_mainRect.Contains(new IntPoint(blockX, blockY)))
			{
				if (allowResize)
					Resize(blockX, blockY);
				else
					return null;
			}

			blockX -= m_mainRect.X1;
			blockY -= m_mainRect.Y1;

			var block = m_grid[blockX, blockY];

			if (block == null)
			{
				if (!allowResize)
					return null;

				block = new Block(m_blockSize);
				m_grid[blockX, blockY] = block;
			}

			return block;
		}

		void Resize(int blockX, int blockY)
		{
			int rx, ry, rw, rh;

			if (blockX < m_mainRect.X1)
			{
				rx = blockX;
				rw = m_mainRect.Width + (m_mainRect.X1 - blockX);
			}
			else if (blockX > m_mainRect.X2 - 1)
			{
				rx = m_mainRect.X;
				rw = m_mainRect.Width + (blockX - (m_mainRect.X2 - 1));
			}
			else
			{
				rx = m_mainRect.X;
				rw = m_mainRect.Width;
			}

			if (blockY < m_mainRect.Y1)
			{
				ry = blockY;
				rh = m_mainRect.Height + (m_mainRect.Y1 - blockY);
			}
			else if (blockY > m_mainRect.Y2 - 1)
			{
				ry = m_mainRect.Y;
				rh = m_mainRect.Height + (blockY - (m_mainRect.Y2 - 1));
			}
			else
			{
				ry = m_mainRect.Y;
				rh = m_mainRect.Height;
			}

			IntRect newRect = new IntRect(rx, ry, rw, rh);

			MainGrid newGrid = new MainGrid(newRect.Width, newRect.Height);

			for (int y = 0; y < m_mainRect.Height; y++)
			{
				for (int x = 0; x < m_mainRect.Width; x++)
				{
					newGrid[x - (newRect.X - m_mainRect.X), y - (newRect.Y - m_mainRect.Y)] = m_grid[x, y];
				}
			}

			m_mainRect = newRect;
			m_grid = newGrid;
		}
	}
}
