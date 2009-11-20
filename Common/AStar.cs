using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;
using System.Diagnostics;

namespace MyGame
{
	public static class AStar
	{
		// tries to save some memory by using ushorts
		// public, so that AStarTest can show the internals
		public class Node
		{
			public IntPoint Loc { get; private set; }
			public Node Parent;
			public ushort G;
			public ushort H;
			public ushort F { get { return (ushort)(G + H); } }
			public bool Closed { get; set; }

			public Node(IntPoint l, Node parent)
			{
				Loc = l;
				Parent = parent;
			}
		}

		// public for AStarTest
		public static IDictionary<IntPoint,Node> FindPathNodeMap(IntPoint src, IntPoint dst, Func<IntPoint, bool> locValid)
		{
			Node lastNode;
			var nodes = FindPathInternal(src, dst, true, locValid, out lastNode);
			return nodes;
		}

		// public for AStarTest
		public static IEnumerable<Node> FindPathNodes(IntPoint src, IntPoint dst, Func<IntPoint, bool> locValid)
		{
			Node lastNode;
			var nodes = FindPathInternal(src, dst, true, locValid, out lastNode);
			if (nodes == null)
				return null;
			return nodes.Values;
		}
		
		public static IEnumerable<Direction> FindPathReverse(IntPoint src, IntPoint dst, Func<IntPoint, bool> locValid)
		{
			Node lastNode;
			var nodes = FindPathInternal(src, dst, true, locValid, out lastNode);
			if(nodes == null)
				yield break;

			Node n = nodes[dst];
			while (n.Parent != null)
			{
				yield return (n.Parent.Loc - n.Loc).ToDirection();
				n = n.Parent;
			}
		}

		public static IEnumerable<Direction> FindPath(IntPoint src, IntPoint dst, bool exactLocation,
			Func<IntPoint, bool> locValid)
		{
			Node lastNode;
			var nodes = FindPathInternal(src, dst, exactLocation, locValid, out lastNode);
			if (nodes == null)
				yield break;

			Node n1 = null;
			Node n2 = lastNode;
			Node n3 = n2.Parent;
			while (n3 != null)
			{
				n2.Parent = n1;

				n1 = n2;
				n2 = n3;
				n3 = n3.Parent;
			}
			n2.Parent = n1;

			var n = nodes[src];
			while (n.Parent != null)
			{
				yield return (n.Parent.Loc - n.Loc).ToDirection();
				n = n.Parent;
			}
		}

		static IDictionary<IntPoint, Node> FindPathInternal(IntPoint src, IntPoint dst, bool exactLocation,
			Func<IntPoint, bool> locValid, out Node lastNode)
		{
			OpenList.Test();
			/*
			Stopwatch sw = new Stopwatch();
			sw.Start();
			*/
			lastNode = null;

			var openList = new OpenList();
			var nodeMap = new Dictionary<IntPoint, Node>();

			var node = new Node(src, null);
			openList.Add(node);
			nodeMap.Add(src, node);

			while (!openList.IsEmpty)
			{
				node = openList.Pop();
				node.Closed = true;

				if (exactLocation && node.Loc == dst)
				{
					lastNode = node;
					break;
				}

				if (!exactLocation && (node.Loc - dst).IsAdjacent)
				{
					lastNode = node;
					break;
				}

				CheckNeighbors(node, dst, openList, nodeMap, locValid);
			}
			/*
			sw.Stop();
			Console.WriteLine(sw.ElapsedTicks.ToString());
			 */
			// 1472794 ticks
			// 329821
			// 249457 ticks
			// 143858

			if (exactLocation && node.Loc != dst)
				return null;

			if (!exactLocation && !(node.Loc - dst).IsAdjacent)
				return null;

			return nodeMap;
		}

		static ushort CostBetweenNodes(IntPoint from, IntPoint to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)10 : (ushort)14;
			return cost;
		}

		static void CheckNeighbors(Node node, IntPoint dst, OpenList openList,
			IDictionary<IntPoint, Node> nodeMap, Func<IntPoint, bool> locValid)
		{
			foreach (IntVector v in IntVector.GetAllXYDirections())
			{
				IntPoint newLoc = node.Loc + v;
				if (!locValid(newLoc))
					continue;

				Node oldNode;
				nodeMap.TryGetValue(newLoc, out oldNode);
				if (oldNode != null && oldNode.Closed)
					continue;

				ushort g = CostBetweenNodes(node.Loc, newLoc);
				ushort h = (ushort)((dst - newLoc).ManhattanLength * 10);

				if (oldNode == null)
				{
					var newNode = new Node(newLoc, node);
					newNode.G = g;
					newNode.H = h;
					openList.Add(newNode);
					nodeMap.Add(newLoc, newNode);
				}
				else if (oldNode.G > g + CostBetweenNodes(oldNode.Loc, newLoc))
				{
					oldNode.Parent = node;
					oldNode.G = g;
					openList.NodeUpdated(oldNode);
				}

			}
		}

		class OpenList : IEnumerable<Node>
		{
			Node[] m_openList = new Node[128];
			int m_count;

			public bool IsEmpty { get { return m_count == 0; } }

			public void Add(Node node)
			{
				if (m_count == 0)
				{
					m_openList[0] = node;
					m_count++;
					return;
				}

				if (m_count >= m_openList.Length)
				{
					Node[] newArray = new Node[m_openList.Length * 2];
					m_openList.CopyTo(newArray, 0);
					m_openList = newArray;
				}

				int m = m_count;

				Debug.Assert(m_openList[m] == null);
				m_openList[m] = node;

				while (m > 0)
				{
					if (m_openList[m].F > m_openList[(m - 1) / 2].F)
						break;

					Node n = m_openList[(m - 1) / 2];
					m_openList[(m - 1) / 2] = m_openList[m];
					m_openList[m] = n;
					m = (m - 1) / 2;
				}

				m_count++;
			}

			public Node Pop()
			{
				Node ret = m_openList[0];

				m_count--;

				if (m_count == 0)
				{
					m_openList[0] = null;
					return ret;
				}

				m_openList[0] = m_openList[m_count];
				m_openList[m_count] = null;

				int v = 0;

				while (true)
				{
					int u = v;

					if (2 * u + 2 < m_count)
					{
						// both children exist

						if (m_openList[u].F >= m_openList[2 * u + 1].F)
							v = 2 * u + 1;

						if (m_openList[v].F >= m_openList[2 * u + 2].F)
							v = 2 * u + 2;
					}
					else if (2 * u + 1 < m_count)
					{
						// one child exists

						if (m_openList[u].F >= m_openList[2 * u + 1].F)
							v = 2 * u + 1;
					}

					if (u != v)
					{
						Node n = m_openList[u];
						m_openList[u] = m_openList[v];
						m_openList[v] = n;
					}
					else
					{
						break;
					}
				}

				return ret;
			}

			// F changed
			public void NodeUpdated(Node node)
			{
				throw new NotImplementedException();
			}

			#region IEnumerable<Node> Members

			public IEnumerator<Node> GetEnumerator()
			{
				return m_openList.Where(n => n != null).AsEnumerable().GetEnumerator();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return m_openList.Where(n => n != null).GetEnumerator();
			}

			#endregion

			[Conditional("DEBUG")]
			public static void Test()
			{
				var openList = new OpenList();
				var testList = new List<int>();
				Random rand = new Random();
				ushort val;
				for (int i = 0; i < 100; ++i)
				{
					val = (ushort)rand.Next(100);
					openList.Add(new Node(new IntPoint(), null) { G = val });
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
}
