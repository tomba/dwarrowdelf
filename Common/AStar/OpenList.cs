using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.AStar
{
	interface IAStarNode
	{
		ushort G { get; }
		ushort H { get; }
		ushort F { get; }
	}

	interface IOpenList<T>
	{
		bool IsEmpty { get; }
		void Add(T node);
		T Pop();
		void NodeUpdated(T node);
	}

	class SimpleOpenList<T> : IOpenList<T> where T : IAStarNode
	{
		List<T> m_list = new List<T>(128);

		public bool IsEmpty
		{
			get { return m_list.Count == 0; }
		}

		public void Add(T node)
		{
			m_list.Add(node);
			m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
		}

		public T Pop()
		{
			var node = m_list.First();
			m_list.RemoveAt(0);
			return node;
		}

		public void NodeUpdated(T node)
		{
			Debug.Assert(m_list.Contains(node));
			m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
		}
	}

	class BinaryHeap<T> : IOpenList<T> where T : class, IAStarNode
	{
		static BinaryHeap()
		{
			BinaryHeap<T>.Test();
		}

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

		// XXX recursive
		void HeapifyDown(int i)
		{
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

		[Conditional("DEBUG")]
		public static void Test()
		{
			var openList = new BinaryHeap<AStar2DNode>();
			var testList = new List<int>();
			Random rand = new Random();
			ushort val;

			ushort[] arr1 = { 32, 28, 71, 71, 71, 81, 89, 70, 92, 69, 96, 52, 30 };
			ushort[] arr2 = new ushort[arr1.Length];

			foreach (ushort u in arr1)
				openList.Add(new AStar2DNode(new IntPoint(), null) { G = u });

			{
				var n = openList.m_openList[5];
				n.G = 91;
				openList.NodeUpdated(n);
			}

			for (int i = 0; i < arr2.Length; ++i)
				arr2[i] = openList.Pop().G;


			for (int i = 0; i < 100; ++i)
			{
				val = (ushort)rand.Next(100);
				openList.Add(new AStar2DNode(new IntPoint(), null) { G = val });
				testList.Add(val);

				if (i % 20 == 19)
				{
					testList.Sort();
					for (int t = 0; t < 5; ++t)
					{
						int v1 = openList.Pop().F;
						int v2 = testList[0];
						testList.RemoveAt(0);

						if (v1 != v2)
							throw new Exception();

					}
				}
			}

			testList.Sort();

			while (!openList.IsEmpty)
			{
				int v1 = openList.Pop().F;
				int v2 = testList[0];
				testList.RemoveAt(0);

				if (v1 != v2)
					throw new Exception();

			}
		}
	}
}
