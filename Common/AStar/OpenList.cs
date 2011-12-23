using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.AStar
{
	interface IOpenListNode
	{
		int F { get; }
	}

	interface IOpenList<T> where T : class, IOpenListNode
	{
		bool IsEmpty { get; }
		void Add(T node);
		T Pop();
		void NodeUpdated(T node);
	}

	sealed class BinaryHeap<T> : IOpenList<T> where T : class, IOpenListNode
	{
		T[] m_openList = new T[128];
		int m_count;

		public bool IsEmpty { get { return m_count == 0; } }

		int Parent(int idx) { return idx / 2; }
		int Left(int idx) { return idx * 2; }
		int Right(int idx) { return idx * 2 + 1; }

		public void Add(T node)
		{
			if (m_count + 1 == m_openList.Length)
			{
				T[] newArray = new T[m_openList.Length * 2];
				m_openList.CopyTo(newArray, 0);
				m_openList = newArray;
			}

			m_count = m_count + 1;
			int i = m_count;

			HeapifyUp(i, node);
		}

		void HeapifyUp(int i, T node)
		{
			while (i > 1)
			{
				int pi = Parent(i);
				var pn = m_openList[pi];

				if (pn.F <= node.F)
					break;

				m_openList[i] = pn;
				i = pi;
			}

			m_openList[i] = node;
		}

		public T Pop()
		{
			T ret = m_openList[1];

			m_openList[1] = m_openList[m_count];

			m_openList[m_count] = null;

			m_count = m_count - 1;

			HeapifyDown(1);

			return ret;
		}

		void HeapifyDown(int i)
		{
#if USE_RECURSIVE
			int li = Left(i);
			int ri = Right(i);
			int lowest;

			if (li <= m_count && m_openList[li].F < m_openList[i].F)
				lowest = li;
			else
				lowest = i;

			if (ri <= m_count && m_openList[ri].F < m_openList[lowest].F)
				lowest = ri;

			if (lowest != i)
			{
				T n = m_openList[lowest];
				m_openList[lowest] = m_openList[i];
				m_openList[i] = n;
				HeapifyDown(lowest);
			}
#else
			T n = m_openList[i];

			while (i * 2 <= m_count)
			{
				int lowest;
				int li = Left(i);
				int ri = Right(i);

				if (ri <= m_count && m_openList[ri].F < m_openList[li].F)
					lowest = ri;
				else
					lowest = li;

				if (m_openList[lowest].F < n.F)
					m_openList[i] = m_openList[lowest];
				else
					break;

				i = lowest;
			}

			m_openList[i] = n;
#endif
		}

		// F changed
		public void NodeUpdated(T node)
		{
			int i;

			for (i = 1; i <= m_count; ++i)
			{
				if (m_openList[i] == node)
					break;
			}

			if (i > m_count)
				throw new Exception();

			HeapifyUp(i, node);
		}
	}
}
