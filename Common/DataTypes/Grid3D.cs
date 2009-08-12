using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	/**
	 * 3D grid, lazily allocated, growable. Made of 2D blocks.
	 */
	public class Grid3D<T>
	{
		GrowingLocationGrid<T>[] m_grid;
		int m_originZ;
		int m_blockSize;

		public Grid3D(int blockSize)
		{
			m_blockSize = blockSize;
		}

		public T this[IntPoint3D l]
		{
			get
			{
				int x = l.X;
				int y = l.Y;
				int z = l.Z;
				T[,] block = GetBlock(ref x, ref y, z, false);
				if (block == null)
					throw new IndexOutOfRangeException("Resize not allowed");

				return block[x, y];
			}

			set
			{
				int x = l.X;
				int y = l.Y;
				int z = l.Z;
				T[,] block = GetBlock(ref x, ref y, z, true);
				block[x, y] = value;
			}
		}

		public T this[int x, int y, int z]
		{
			get { return this[new IntPoint3D(x, y, z)]; }
			set { this[new IntPoint3D(x, y, z)] = value; }
		}

		T[,] GetBlock(ref int x, ref int y, int z, bool allowResize)
		{
			if (m_grid == null)
			{
				if (!allowResize)
					return null;

				m_grid = new GrowingLocationGrid<T>[0];
				m_originZ = -z;
			}

			int idx = z + m_originZ;

			if (idx < 0 || idx >= m_grid.Length)
			{
				if (!allowResize)
					return null;

				Resize(z);
			}

			idx = z + m_originZ;

			if (m_grid[idx] == null)
			{
				if (!allowResize)
					return null;

				m_grid[idx] = new GrowingLocationGrid<T>(m_blockSize);
			}

			return m_grid[idx].GetBlock(ref x, ref y, allowResize);
		}

		void Resize(int z)
		{
			int idx = z + m_originZ;
			int newLen;
			int newOffset;

			if (idx < 0)
			{
				newLen = m_grid.Length - idx;
				newOffset = -z;
			}
			else if (idx >= m_grid.Length)
			{
				newLen = m_grid.Length + (idx - m_grid.Length + 1);
				newOffset = m_originZ;
			}
			else
			{
				throw new Exception();
			}

			GrowingLocationGrid<T>[] newArray = new GrowingLocationGrid<T>[newLen];

			for (idx = 0; idx < m_grid.Length; idx++)
				newArray[(newOffset - m_originZ) + idx] = m_grid[idx];

			m_grid = newArray;
			m_originZ = newOffset;
		}
	}
}
