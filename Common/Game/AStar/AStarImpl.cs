using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.AStar
{
	// tries to save some memory by using ushorts.
	public sealed class AStarNode : IOpenListNode
	{
		public IntPoint3 Loc { get; private set; }
		public AStarNode Parent;
		public ushort G { get; set; }
		public ushort H { get; set; }
		public bool Closed { get; set; }

		public int F { get { return G + H; } }

		public AStarNode(IntPoint3 l, AStarNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public sealed class AStarImpl
	{
		public const int COST_DIAGONAL = 14;
		public const int COST_STRAIGHT = 10;

		readonly CancellationToken m_cancellationToken;
		readonly int m_maxNodeCount;

		readonly Func<IntPoint3, int> m_getTileWeight;
		readonly Func<IntPoint3, IEnumerable<Direction>> m_getValidDirs;

		readonly IAStarTarget m_target;

		readonly IOpenList<AStarNode> m_openList;
		readonly Dictionary<IntPoint3, AStarNode> m_nodeMap;

		public Dictionary<IntPoint3, AStarNode> Nodes { get { return m_nodeMap; } }
		public AStarNode LastNode { get; private set; }

		public Action<Dictionary<IntPoint3, AStarNode>> DebugCallback { get; set; }

		public AStarImpl(Func<IntPoint3, int> getTileWeight, Func<IntPoint3, IEnumerable<Direction>> getValidDirs,
			IEnumerable<IntPoint3> initialLocations,
			IAStarTarget target, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			m_getTileWeight = getTileWeight;
			m_getValidDirs = getValidDirs;
			m_maxNodeCount = maxNodeCount;
			m_cancellationToken = cancellationToken.HasValue ? cancellationToken.Value : CancellationToken.None;

			m_target = target;
			m_nodeMap = new Dictionary<IntPoint3, AStarNode>();
			m_openList = new BinaryHeap<AStarNode>();

			foreach (var p in initialLocations)
			{
				ushort g = 0;
				ushort h = m_target.GetHeuristic(p);

				var node = new AStarNode(p, null);
				node.G = g;
				node.H = h;
				m_openList.Add(node);
				m_nodeMap.Add(p, node);
			}
		}

		public AStarStatus Find()
		{
			//Debug.Print("Start");

			var nodeMap = m_nodeMap;
			var openList = m_openList;

			// If this is not the first loop, check the neighbors of the last node
			if (this.LastNode != null)
				CheckNeighbors(this.LastNode);

			while (!openList.IsEmpty)
			{
				if (m_cancellationToken.IsCancellationRequested)
					return AStarStatus.Cancelled;

				if (nodeMap.Count > m_maxNodeCount)
					return AStarStatus.LimitExceeded;

				if (DebugCallback != null)
					DebugCallback(nodeMap);

				var node = openList.Pop();
				node.Closed = true;

				if (m_target.GetIsTarget(node.Loc))
				{
					this.LastNode = node;
					return AStarStatus.Found;
				}

				CheckNeighbors(node);
			}

			return AStarStatus.NotFound;
		}

		static ushort CostBetweenNodes(IntPoint3 from, IntPoint3 to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)COST_STRAIGHT : (ushort)COST_DIAGONAL;
			return cost;
		}

		void CheckNeighbors(AStarNode parent)
		{
			foreach (var dir in m_getValidDirs(parent.Loc))
			{
				IntPoint3 childLoc = parent.Loc + new IntVector3(dir);

				AStarNode child;
				m_nodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc));
				if (m_getTileWeight != null)
					g += (ushort)m_getTileWeight(childLoc);
				ushort h = m_target.GetHeuristic(childLoc);

				if (child == null)
				{
					child = new AStarNode(childLoc, parent);
					child.G = g;
					child.H = h;
					m_openList.Add(child);
					m_nodeMap.Add(childLoc, child);
				}
				else if (child.G > g)
				{
					child.Parent = parent;
					child.G = g;
					//Debug.Print("{0} update", child.Loc);

					if (child.Closed == false)
						m_openList.NodeUpdated(child);
					else // Closed == true
						UpdateParents(child);
				}
			}
		}

		void UpdateParents(AStarNode parent)
		{
			//Debug.Print("updating closed node {0}", parent.Loc);

			Stack<AStarNode> queue = new Stack<AStarNode>();

			UpdateNodes(parent, queue);

			while (queue.Count > 0)
			{
				parent = queue.Pop();

				UpdateNodes(parent, queue);
			}
		}

		void UpdateNodes(AStarNode parent, Stack<AStarNode> queue)
		{
			foreach (var dir in m_getValidDirs(parent.Loc))
			{
				IntPoint3 childLoc = parent.Loc + new IntVector3(dir);

				AStarNode child;
				m_nodeMap.TryGetValue(childLoc, out child);
				if (child == null)
					continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc));
				if (m_getTileWeight != null)
					g += (ushort)m_getTileWeight(childLoc);

				if (g < child.G)
				{
					//Debug.Print("closed node {0} updated 1", childLoc);

					child.Parent = parent;
					child.G = g;

					queue.Push(child);
				}
			}
		}
	}
}
