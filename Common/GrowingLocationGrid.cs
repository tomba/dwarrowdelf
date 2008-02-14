using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class GrowingLocationGrid<T>
	{
		T[,][,] m_grid;

		IntRect m_mainRect;
		int m_blockSize;

		public GrowingLocationGrid(int blockSize)
		{
			m_blockSize = blockSize;
			m_mainRect = new IntRect(0, 0, 1, 1);
			m_grid = new T[m_mainRect.Width, m_mainRect.Height][,];
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

		protected T[,][,] Grid
		{
			get { return m_grid; }
		}

		public T this[int x, int y]
		{
			get
			{
				int blockX;
				int blockY;

				if (Ensure(ref x, ref y, out blockX, out blockY, false) == false)
					throw new Exception("resize not allowed");

				return m_grid[blockX, blockY][x, y];
			}

			set
			{
				int blockX;
				int blockY;

				Ensure(ref x, ref y, out blockX, out blockY, true);

				m_grid[blockX, blockY][x, y] = value;
			}
		}

		protected bool Ensure(ref int x, ref int y, out int blockX, out int blockY, bool allowResize)
		{
			blockX = Math.DivRem(x, m_blockSize, out x);
			blockY = Math.DivRem(y, m_blockSize, out y);

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

			if (!m_mainRect.Contains(new Location(blockX, blockY)))
			{
				if (allowResize)
					Resize(blockX, blockY);
				else
					return false;
			}

			blockX -= m_mainRect.Left;
			blockY -= m_mainRect.Top;

			if (m_grid[blockX, blockY] == null)
				m_grid[blockX, blockY] = new T[m_blockSize, m_blockSize];

			return true;
		}

		protected T[,] GetBlock(ref int x, ref int y, bool allowResize, bool allowCreate)
		{
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

			if (!m_mainRect.Contains(new Location(blockX, blockY)))
			{
				if (allowResize)
					Resize(blockX, blockY);
				else
					return null;
			}

			blockX -= m_mainRect.Left;
			blockY -= m_mainRect.Top;

			if (m_grid[blockX, blockY] == null)
				m_grid[blockX, blockY] = new T[m_blockSize, m_blockSize];

			return m_grid[blockX, blockY];
		}

		public T this[Location l]
		{
			get { return this[l.X, l.Y]; }
			set { this[l.X, l.Y] = value; }
		}

		void Resize(int blockX, int blockY)
		{
			IntRect newRect = m_mainRect;

			if (blockX < newRect.Left)
			{
				newRect.Width += (newRect.Left - blockX);
				newRect.X = blockX;
			}
			else if(blockX > newRect.Right - 1)
			{
				newRect.Width += (blockX - (newRect.Right - 1));
			}

			if (blockY < newRect.Top)
			{
				newRect.Height += (newRect.Top - blockY);
				newRect.Y = blockY;
			}
			else if (blockY > newRect.Bottom - 1)
			{
				newRect.Height += (blockY - (newRect.Bottom - 1));
			}

			T[,][,] newGrid = new T[newRect.Width, newRect.Height][,];

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
