using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AStar
{
	public class AStar2DResult
	{
		public IDictionary<IntPoint, AStar2DNode> Nodes { get; private set; }
		public AStar2DNode LastNode { get; private set; }
		public bool PathFound { get { return this.LastNode != null; } }

		internal AStar2DResult(IDictionary<IntPoint, AStar2DNode> nodes, AStar2DNode lastNode)
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

			AStar2DNode n = this.LastNode;
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
	public class AStar2DNode : IAStarNode
	{
		public IntPoint Loc { get; private set; }
		public AStar2DNode Parent;
		public ushort G { get; set; }
		public ushort H { get; set; }
		public ushort F { get { return (ushort)(G + H); } }
		public bool Closed { get; set; }

		public AStar2DNode(IntPoint l, AStar2DNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public static class AStar2D
	{
		class AStarState
		{
			public IntPoint Src;
			public IntPoint Dst;
			public IOpenList<AStar2DNode> OpenList;
			public IDictionary<IntPoint, AStar2DNode> NodeMap;
			public Func<IntPoint, int> TileWeight;
			public Func<IntPoint, IEnumerable<Direction>> GetValidDirs;
		}

		public static AStar2DResult Find(IntPoint src, IntPoint dst, bool exactLocation, Func<IntPoint, int> tileWeight,
			Func<IntPoint, IEnumerable<Direction>> validDirs)
		{
			var state = new AStarState()
			{
				Src = src,
				Dst = dst,
				TileWeight = tileWeight,
				GetValidDirs = validDirs,
				NodeMap = new Dictionary<IntPoint, AStar2DNode>(),
				OpenList = new BinaryHeap<AStar2DNode>(),
				//OpenList = new SimpleOpenList<AStar2DNode>(),
			};

			AStar2DNode lastNode;
			var nodes = FindInternal(state, exactLocation, out lastNode);
			return new AStar2DResult(nodes, lastNode);
		}

		static IDictionary<IntPoint, AStar2DNode> FindInternal(AStarState state, bool exactLocation, out AStar2DNode lastNode)
		{
			lastNode = null;

			//MyTrace.WriteLine("Start");

			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			var node = new AStar2DNode(state.Src, null);
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

		static void CheckNeighbors(AStarState state, AStar2DNode parent)
		{
			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint childLoc = parent.Loc + IntVector.FromDirection(dir);

				AStar2DNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.TileWeight(childLoc));
				ushort h = (ushort)((state.Dst - childLoc).ManhattanLength * 10);

				if (child == null)
				{
					child = new AStar2DNode(childLoc, parent);
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

		static void UpdateParents(AStarState state, AStar2DNode parent)
		{
			//MyTrace.WriteLine("updating closed node {0}", parent.Loc);

			Stack<AStar2DNode> queue = new Stack<AStar2DNode>();

			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint childLoc = parent.Loc + IntVector.FromDirection(dir);

				AStar2DNode child;
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

				foreach (var dir in state.GetValidDirs(parent.Loc))
				{
					IntPoint childLoc = parent.Loc + IntVector.FromDirection(dir);

					AStar2DNode child;
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
	}

}
