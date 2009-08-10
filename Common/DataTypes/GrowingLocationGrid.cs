﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	/**
	 * 2D grid, made of smaller blocks, allowing the grid to grow
	 */
	public class GrowingLocationGrid<T>
	{
		T[,][,] m_grid;

		// m_mainRect tells the size of m_grid, but also the origin
		IntRect m_mainRect;
		int m_blockSize;

		public GrowingLocationGrid(int blockSize)
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

		public T this[int x, int y]
		{
			get
			{
				T[,] block = GetBlock(ref x, ref y, false);
				if (block == null)
					throw new IndexOutOfRangeException("Resize not allowed");

				return block[x, y];
			}

			set
			{
				T[,] block = GetBlock(ref x, ref y, true);
				block[x, y] = value;
			}
		}

		public T this[IntPoint l]
		{
			get { return this[l.X, l.Y]; }
			set { this[l.X, l.Y] = value; }
		}

		public T[,] GetBlock(ref IntPoint l, bool allowResize)
		{
			int x = l.X;
			int y = l.Y;

			T[,] block = GetBlock(ref x, ref y, allowResize);

			l.X = x;
			l.Y = y;

			return block;
		}

		public T[,] GetBlock(ref int x, ref int y, bool allowResize)
		{
			if (m_grid == null)
			{
				m_mainRect = new IntRect(x / m_blockSize, y / m_blockSize, 1, 1);
				m_grid = new T[1, 1][,];
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

			blockX -= m_mainRect.Left;
			blockY -= m_mainRect.Top;

			if (m_grid[blockX, blockY] == null)
			{
				if (!allowResize)
					return null;

				m_grid[blockX, blockY] = new T[m_blockSize, m_blockSize];
			}

			return m_grid[blockX, blockY];
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
