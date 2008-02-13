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

		int m_blockWidth;
		int m_blockHeight;

		public Location Origin { get; set; }

		public GrowingLocationGrid(int blockWidth, int blockHeight)
		{
			m_blockWidth = blockWidth;
			m_blockHeight = blockHeight;
			m_mainRect = new IntRect(0, 0, 1, 1);
			ReallocateGrid();
		}

		void ReallocateGrid()
		{
			m_grid = new T[m_mainRect.Width, m_mainRect.Height][,];
		}

		public T this[int x, int y]
		{
			get
			{
				int blockX = Math.DivRem(x, m_blockHeight, out x) - m_mainRect.Left;
				int blockY = Math.DivRem(y, m_blockWidth, out y) - m_mainRect.Top;
				return m_grid[blockX, blockY][x, y];
			}

			set
			{
				int blockX = Math.DivRem(x, m_blockHeight, out x) - m_mainRect.Left;
				int blockY = Math.DivRem(y, m_blockWidth, out y) - m_mainRect.Top;

				if (!m_mainRect.Contains(new Location(blockX, blockY)))
				{
					Resize(blockX, blockY);

					blockX = Math.DivRem(x, m_blockHeight, out x) - m_mainRect.Left;
					blockY = Math.DivRem(y, m_blockWidth, out y) - m_mainRect.Top;
				}

				if(m_grid[blockX, blockY] == null)
					m_grid[blockX, blockY] = new T[m_blockWidth, m_blockHeight];
				m_grid[blockX, blockY][x, y] = value;
			}
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
