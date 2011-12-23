using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.AStar
{
	public interface IAStarEnvironment
	{
		int GetTileWeight(IntPoint3D p);
		IEnumerable<Direction> GetValidDirs(IntPoint3D p);
		bool CanEnter(IntPoint3D p);
		// XXX Callback for single-stepping. Remove at some point.
		void Callback(IDictionary<IntPoint3D, AStarNode> nodes);
	}

	// tries to save some memory by using ushorts.
	public sealed class AStarNode : IOpenListNode
	{
		public IntPoint3D Loc { get; private set; }
		public AStarNode Parent;
		public ushort G { get; set; }
		public ushort H { get; set; }
		public bool Closed { get; set; }

		public int F { get { return G + H; } }

		public AStarNode(IntPoint3D l, AStarNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public static class AStarFinder
	{
		class AStarState
		{
			public IAStarEnvironment Environment;
			public IAStarTarget Target;
			public IntPoint3D Src;
			public DirectionSet SrcPositioning;
			public IOpenList<AStarNode> OpenList;
			public IDictionary<IntPoint3D, AStarNode> NodeMap;
			public CancellationToken CancellationToken;
		}


		public static IEnumerable<Direction> Find(IAStarEnvironment environment, IntPoint3D src, IntPoint3D dest, DirectionSet positioning, out IntPoint3D finalLocation)
		{
			Debug.Assert(environment != null);

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult resBackward = null;
			AStarResult resForward = null;

			var taskForward = new Task(delegate
			{
				resForward = Find(environment, src, DirectionSet.Exact, dest, positioning, 200000, cts.Token);
			});
			taskForward.Start();

			var taskBackward = new Task(delegate
			{
				resBackward = Find(environment, dest, positioning, src, DirectionSet.Exact, 200000, cts.Token);
			});
			taskBackward.Start();

			Task.WaitAny(taskBackward, taskForward);

			cts.Cancel();

			Task.WaitAll(taskBackward, taskForward);

			IEnumerable<Direction> dirs;

			if (resForward.Status == AStarStatus.Found)
			{
				dirs = resForward.GetPath();
				finalLocation = resForward.LastNode.Loc;
			}
			else if (resBackward.Status == AStarStatus.Found)
			{
				dirs = resBackward.GetPathReverse();

				AStarNode n = resBackward.LastNode;
				while (n.Parent != null)
					n = n.Parent;

				finalLocation = n.Loc;
			}
			else
			{
				dirs = null;
				finalLocation = new IntPoint3D();
			}

			return dirs;
		}


		public static AStarResult FindNearest(IAStarEnvironment environment, IntPoint3D src, Func<IntPoint3D, bool> func, int maxNodeCount = 200000)
		{
			return Find(environment, src, DirectionSet.Exact, new AStarDelegateTarget(func), maxNodeCount);
		}

		public static AStarResult Find(IAStarEnvironment environment, IntPoint3D src, DirectionSet srcPositioning, IntPoint3D dst, DirectionSet dstPositioning,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return Find(environment, src, srcPositioning, new AStarDefaultTarget(dst, dstPositioning), maxNodeCount, cancellationToken);
		}

		public static AStarResult Find(IAStarEnvironment environment, IntPoint3D src, DirectionSet srcPositioning, IAStarTarget target,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var state = new AStarState()
			{
				Environment = environment,
				Src = src,
				SrcPositioning = srcPositioning,
				Target = target,
				NodeMap = new Dictionary<IntPoint3D, AStarNode>(),
				OpenList = new BinaryHeap<AStarNode>(),
				CancellationToken = cancellationToken.HasValue ? cancellationToken.Value : CancellationToken.None,
			};

			return FindInternal(state, maxNodeCount);
		}

		static void AddInitialNodes(AStarState state)
		{
			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			IEnumerable<IntPoint3D> nodeList;

			nodeList = state.SrcPositioning.ToDirections().Select(d => state.Src + d);

			foreach (var p in nodeList.Where(p => state.Environment.CanEnter(p)))
			{
				ushort g = 0;
				ushort h = state.Target.GetHeuristic(p);

				var node = new AStarNode(p, null);
				node.G = g;
				node.H = h;
				openList.Add(node);
				nodeMap.Add(p, node);
			}
		}

		static AStarResult FindInternal(AStarState state, int maxNodeCount)
		{
			//MyTrace.WriteLine("Start");

			AddInitialNodes(state);

			AStarNode lastNode = null;
			var status = AStarStatus.NotFound;
			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			while (!openList.IsEmpty)
			{
				if (state.CancellationToken.IsCancellationRequested)
				{
					status = AStarStatus.Cancelled;
					break;
				}

				state.Environment.Callback(nodeMap);

				var node = openList.Pop();
				node.Closed = true;

				if (state.Target.GetIsTarget(node.Loc))
				{
					lastNode = node;
					status = AStarStatus.Found;
					break;
				}

				CheckNeighbors(state, node);

				if (nodeMap.Count > maxNodeCount)
				{
					status = AStarStatus.LimitExceeded;
					break;
				}
			}

			if (status == AStarStatus.LimitExceeded)
				Trace.TraceWarning("AStar3D: Limit Exceeded");
			else if (status == AStarStatus.NotFound)
				Trace.TraceWarning("AStar3D: Not Found");

			return new AStarResult(nodeMap, lastNode, status);
		}

		public const int COST_DIAGONAL = 14;
		public const int COST_STRAIGHT = 10;

		static ushort CostBetweenNodes(IntPoint3D from, IntPoint3D to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)COST_STRAIGHT : (ushort)COST_DIAGONAL;
			return cost;
		}

		static void CheckNeighbors(AStarState state, AStarNode parent)
		{
			foreach (var dir in state.Environment.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

				AStarNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.Environment.GetTileWeight(childLoc));
				ushort h = state.Target.GetHeuristic(childLoc);

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

			foreach (var dir in state.Environment.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

				AStarNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				if (child == null)
					continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.Environment.GetTileWeight(childLoc));

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

				foreach (var dir in state.Environment.GetValidDirs(parent.Loc))
				{
					IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

					AStarNode child;
					state.NodeMap.TryGetValue(childLoc, out child);
					if (child == null)
						continue;

					ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.Environment.GetTileWeight(childLoc));

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
