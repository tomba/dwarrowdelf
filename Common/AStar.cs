using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	public class AStarResult
	{
		public IDictionary<IntPoint, AStarNode> Nodes { get; private set; }
		public AStarNode LastNode { get; private set; }
		public bool PathFound { get { return this.LastNode != null; } }

		internal AStarResult(IDictionary<IntPoint, AStarNode> nodes, AStarNode lastNode)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			this.Nodes = nodes;
			this.LastNode = lastNode;
		}

		public IEnumerable<Direction> GetPathReverse()
		{
			if (this.LastNode == null)
				yield break;

			AStarNode n = this.LastNode;
			while (n.Parent != null)
			{
				yield return (n.Parent.Loc - n.Loc).ToDirection();
				n = n.Parent;
			}
		}

		public IEnumerable<Direction> GetPath()
		{
			return GetPathReverse().Reverse().Select(d => IntVector.FromDirection(d).FastRotate(4).ToDirection());
		}
	}

	// tries to save some memory by using ushorts.
	public class AStarNode
	{
		public IntPoint Loc { get; private set; }
		public AStarNode Parent;
		public ushort G;
		public ushort H;
		public ushort F { get { return (ushort)(G + H); } }
		public bool Closed { get; set; }

		public AStarNode(IntPoint l, AStarNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public static class AStar
	{
		public static AStarResult Find(IntPoint src, IntPoint dst, bool exactLocation, Func<IntPoint, bool> locValid)
		{
			AStarNode lastNode;
			var nodes = FindInternal(src, dst, exactLocation, locValid, out lastNode);
			return new AStarResult(nodes, lastNode);
		}

		static IDictionary<IntPoint, AStarNode> FindInternal(IntPoint src, IntPoint dst, bool exactLocation,
			Func<IntPoint, bool> locValid, out AStarNode lastNode)
		{
			OpenList.Test();

			lastNode = null;

			var nodeMap = new Dictionary<IntPoint, AStarNode>();

			if (exactLocation && !locValid(dst))
				return nodeMap;

			var openList = new OpenList();

			var node = new AStarNode(src, null);
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

			return nodeMap;
		}

		static ushort CostBetweenNodes(IntPoint from, IntPoint to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)10 : (ushort)14;
			return cost;
		}

		static void CheckNeighbors(AStarNode node, IntPoint dst, OpenList openList,
			IDictionary<IntPoint, AStarNode> nodeMap, Func<IntPoint, bool> locValid)
		{
			foreach (IntVector v in IntVector.GetAllXYDirections())
			{
				IntPoint newLoc = node.Loc + v;
				if (!locValid(newLoc))
					continue;

				AStarNode oldNode;
				nodeMap.TryGetValue(newLoc, out oldNode);
				if (oldNode != null && oldNode.Closed)
					continue;

				ushort g = CostBetweenNodes(node.Loc, newLoc);
				ushort h = (ushort)((dst - newLoc).ManhattanLength * 10);

				if (oldNode == null)
				{
					var newNode = new AStarNode(newLoc, node);
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

		class OpenList
		{
			AStarNode[] m_openList = new AStarNode[128];
			int m_count;

			public bool IsEmpty { get { return m_count == 0; } }

			public void Add(AStarNode node)
			{
				if (m_count == 0)
				{
					m_openList[0] = node;
					m_count++;
					return;
				}

				if (m_count >= m_openList.Length)
				{
					AStarNode[] newArray = new AStarNode[m_openList.Length * 2];
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

					AStarNode n = m_openList[(m - 1) / 2];
					m_openList[(m - 1) / 2] = m_openList[m];
					m_openList[m] = n;
					m = (m - 1) / 2;
				}

				m_count++;
			}

			public AStarNode Pop()
			{
				AStarNode ret = m_openList[0];

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
						AStarNode n = m_openList[u];
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
			public void NodeUpdated(AStarNode node)
			{
				throw new NotImplementedException();
			}

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
					openList.Add(new AStarNode(new IntPoint(), null) { G = val });
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
