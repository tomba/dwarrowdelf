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
			return GetPathReverse().Reverse().Select(d => IntVector.FromDirection(d).Reverse().ToDirection());
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
		class AStarState
		{
			public IntPoint Src;
			public IntPoint Dst;
			public IOpenList OpenList;
			public IDictionary<IntPoint, AStarNode> NodeMap;
			public Func<IntPoint, bool> TileValid;
			public Func<IntPoint, int> TileWeight;
		}

		public static AStarResult Find(IntPoint src, IntPoint dst, bool exactLocation, Func<IntPoint, bool> tileValid, Func<IntPoint, int> tileWeight)
		{
			var state = new AStarState()
			{
				Src = src,
				Dst = dst,
				TileValid = tileValid,
				TileWeight = tileWeight,
				NodeMap = new Dictionary<IntPoint, AStarNode>(),
				//OpenList = new BinaryHeap(),
				OpenList = new SimpleOpenList(),
			};

			AStarNode lastNode;
			var nodes = FindInternal(state, exactLocation, out lastNode);
			return new AStarResult(nodes, lastNode);
		}

		static IDictionary<IntPoint, AStarNode> FindInternal(AStarState state, bool exactLocation, out AStarNode lastNode)
		{
			lastNode = null;

			//MyTrace.WriteLine("Start");

			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			if (exactLocation && !state.TileValid(state.Dst))
				return nodeMap;

			var node = new AStarNode(state.Src, null);
			openList.Add(node);
			nodeMap.Add(state.Src, node);

			while (!openList.IsEmpty)
			{
				node = openList.Pop();
				node.Closed = true;

				if (exactLocation && node.Loc == state.Dst)
				{
					lastNode = node;
					break;
				}

				if (!exactLocation && (node.Loc - state.Dst).IsAdjacent)
				{
					lastNode = node;
					break;
				}

				CheckNeighbors(state, node);

				if (nodeMap.Count > 200000)
					throw new Exception();
			}

			return nodeMap;
		}

		static ushort CostBetweenNodes(IntPoint from, IntPoint to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)10 : (ushort)14;
			return cost;
		}

		static void CheckNeighbors(AStarState state, AStarNode parent)
		{
			foreach (IntVector v in IntVector.GetAllXYDirections())
			{
				IntPoint childLoc = parent.Loc + v;
				if (!state.TileValid(childLoc))
					continue;

				AStarNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.TileWeight(childLoc));
				ushort h = (ushort)((state.Dst - childLoc).ManhattanLength * 10);

				if (child == null)
				{
					child = new AStarNode(childLoc, parent);
					child.G = g;
					child.H = h;
					state.OpenList.Add(child);
					state.NodeMap.Add(childLoc, child);
				}
				else if (child.G > g)
				{
					child.Parent = parent;
					child.G = g;
					//MyTrace.WriteLine("{0} update", child.Loc);

					if (child.Closed == false)
						state.OpenList.NodeUpdated(child);
					else // Closed == true
						UpdateParents(state, child);
				}
			}
		}

		static void UpdateParents(AStarState state, AStarNode parent)
		{
			//MyTrace.WriteLine("updating closed node {0}", parent.Loc);

			Stack<AStarNode> queue = new Stack<AStarNode>();

			foreach (IntVector v in IntVector.GetAllXYDirections())
			{
				IntPoint childLoc = parent.Loc + v;
				if (!state.TileValid(childLoc))
					continue;

				AStarNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				if (child == null)
					continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.TileWeight(childLoc));

				if (g < child.G)
				{
					//MyTrace.WriteLine("closed node {0} updated 1", childLoc);

					child.Parent = parent;
					child.G = g;

					queue.Push(child);
				}
			}

			while (queue.Count > 0)
			{
				parent = queue.Pop();

				foreach (IntVector v in IntVector.GetAllXYDirections())
				{
					IntPoint childLoc = parent.Loc + v;
					if (!state.TileValid(childLoc))
						continue;

					AStarNode child;
					state.NodeMap.TryGetValue(childLoc, out child);
					if (child == null)
						continue;

					ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.TileWeight(childLoc));

					if (g < child.G)
					{
						//MyTrace.WriteLine("closed node {0} updated 2", childLoc);

						child.Parent = parent;
						child.G = g;

						queue.Push(child);
					}
				}
			}
		}

		interface IOpenList
		{
			bool IsEmpty { get; }
			void Add(AStarNode node);
			AStarNode Pop();
			void NodeUpdated(AStarNode node);
		}

		class SimpleOpenList : IOpenList
		{
			List<AStarNode> m_list = new List<AStarNode>(128);

			public bool IsEmpty
			{
				get { return m_list.Count == 0; }
			}

			public void Add(AStarNode node)
			{
				m_list.Add(node);
				m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
			}

			public AStarNode Pop()
			{
				var node = m_list.First();
				m_list.RemoveAt(0);
				return node;
			}

			public void NodeUpdated(AStarNode node)
			{
				Debug.Assert(m_list.Contains(node));
				m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
			}
		}

		class BinaryHeap : IOpenList
		{
			static BinaryHeap()
			{
				BinaryHeap.Test();
			}

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
				IOpenList openList = new BinaryHeap();
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
