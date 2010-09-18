using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public abstract class GrowingGrid3DBase<T> where T :class
	{

		T[] m_2DGridList;
		int m_origin;
		int m_blockSize;

		protected GrowingGrid3DBase(int blockSize)
		{
			m_blockSize = blockSize;
		}

		public T GetLevel(int z, bool allowResize)
		{
			if (m_2DGridList == null)
			{
				m_2DGridList = new T[1];
				m_origin = z;
			}

			z -= m_origin;

			if (!allowResize && (z < 0 || z > m_2DGridList.Length))
				return null;

			if (z < 0)
			{
				int diff = -z;
				int newSize = diff + m_2DGridList.Length;
				var newArr = new T[newSize];

				for (int i = 0; i < m_2DGridList.Length; ++i)
					newArr[i + diff] = m_2DGridList[i];

				m_2DGridList = newArr;
				m_origin -= diff;
				z = 0;
			}
			else if(z >= m_2DGridList.Length)
			{
				int diff = z - m_2DGridList.Length;
				int newSize = diff + m_2DGridList.Length + 1;
				var newArr = new T[newSize];

				for (int i = 0; i < m_2DGridList.Length; ++i)
					newArr[i] = m_2DGridList[i];

				m_2DGridList = newArr;
			}

			var grid2d = m_2DGridList[z];

			if (grid2d == null)
			{
				if (!allowResize)
					return null;
				grid2d = CreateLevel(m_blockSize);
				m_2DGridList[z] = grid2d;
			}

			return grid2d;
		}

		protected abstract T CreateLevel(int blockSize);
	}
}