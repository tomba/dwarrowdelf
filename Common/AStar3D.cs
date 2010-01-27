using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	public class AStar3DResult
	{
		public IDictionary<IntPoint3D, AStar3DNode> Nodes { get; private set; }
		public AStar3DNode LastNode { get; private set; }
		public bool PathFound { get { return this.LastNode != null; } }

		internal AStar3DResult(IDictionary<IntPoint3D, AStar3DNode> nodes, AStar3DNode lastNode)
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

			AStar3DNode n = this.LastNode;
			while (n.Parent != null)
			{
				yield return (n.Parent.Loc - n.Loc).ToDirection();
				n = n.Parent;
			}
		}

		public IEnumerable<Direction> GetPath()
		{
			return GetPathReverse().Reverse().Select(d => IntVector3D.FromDirection(d).Reverse().ToDirection());
		}
	}

	// tries to save some memory by using ushorts.
	public class AStar3DNode
	{
		public IntPoint3D Loc { get; private set; }
		public AStar3DNode Parent;
		public ushort G;
		public ushort H;
		public ushort F { get { return (ushort)(G + H); } }
		public bool Closed { get; set; }

		public AStar3DNode(IntPoint3D l, AStar3DNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public static class AStar3D
	{
		class AStarState
		{
			public IntPoint3D Src;
			public IntPoint3D Dst;
			public IOpenList OpenList;
			public IDictionary<IntPoint3D, AStar3DNode> NodeMap;
			public Func<IntPoint3D, int> GetTileWeight;
			public Func<IntPoint3D, IEnumerable<Direction>> GetValidDirs;
		}

		public static AStar3DResult Find(IntPoint3D src, IntPoint3D dst, bool exactLocation, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs)
		{
			var state = new AStarState()
			{
				Src = src,
				Dst = dst,
				GetTileWeight = tileWeight,
				GetValidDirs = validDirs,
				NodeMap = new Dictionary<IntPoint3D, AStar3DNode>(),
				//OpenList = new BinaryHeap(),
				OpenList = new SimpleOpenList(),
			};

			AStar3DNode lastNode;
			var nodes = FindInternal(state, exactLocation, out lastNode);
			return new AStar3DResult(nodes, lastNode);
		}

		static IDictionary<IntPoint3D, AStar3DNode> FindInternal(AStarState state, bool exactLocation, out AStar3DNode lastNode)
		{
			lastNode = null;

			//MyTrace.WriteLine("Start");

			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			var node = new AStar3DNode(state.Src, null);
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

		static ushort CostBetweenNodes(IntPoint3D from, IntPoint3D to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)10 : (ushort)14;
			return cost;
		}

		static void CheckNeighbors(AStarState state, AStar3DNode parent)
		{
			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + IntVector3D.FromDirection(dir);

				AStar3DNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.GetTileWeight(childLoc));
				ushort h = (ushort)((state.Dst - childLoc).ManhattanLength * 10);

				if (child == null)
				{
					child = new AStar3DNode(childLoc, parent);
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

		static void UpdateParents(AStarState state, AStar3DNode parent)
		{
			//MyTrace.WriteLine("updating closed node {0}", parent.Loc);

			Stack<AStar3DNode> queue = new Stack<AStar3DNode>();

			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + IntVector3D.FromDirection(dir);

				AStar3DNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				if (child == null)
					continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.GetTileWeight(childLoc));

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

				foreach (var dir in state.GetValidDirs(parent.Loc))
				{
					IntPoint3D childLoc = parent.Loc + IntVector3D.FromDirection(dir);

					AStar3DNode child;
					state.NodeMap.TryGetValue(childLoc, out child);
					if (child == null)
						continue;

					ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.GetTileWeight(childLoc));

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
			void Add(AStar3DNode node);
			AStar3DNode Pop();
			void NodeUpdated(AStar3DNode node);
		}

		class SimpleOpenList : IOpenList
		{
			List<AStar3DNode> m_list = new List<AStar3DNode>(128);

			public bool IsEmpty
			{
				get { return m_list.Count == 0; }
			}

			public void Add(AStar3DNode node)
			{
				m_list.Add(node);
				m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
			}

			public AStar3DNode Pop()
			{
				var node = m_list.First();
				m_list.RemoveAt(0);
				return node;
			}

			public void NodeUpdated(AStar3DNode node)
			{
				Debug.Assert(m_list.Contains(node));
				m_list.Sort((n1, n2) => n1.F == n2.F ? 0 : (n1.F > n2.F ? 1 : -1));
			}
		}
	}
}
