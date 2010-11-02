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
	public class AStarNode : IOpenListNode
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

	public static class AStar
	{
		class AStarState
		{
			public IAStarTarget Target;
			public IntPoint3D Src;
			public Positioning SrcPositioning;
			public IOpenList<AStarNode> OpenList;
			public IDictionary<IntPoint3D, AStarNode> NodeMap;
			public Func<IntPoint3D, int> GetTileWeight;
			public Func<IntPoint3D, IEnumerable<Direction>> GetValidDirs;
			public CancellationToken CancellationToken;
		}


		public static IEnumerable<Direction> Find(IEnvironment env, IntPoint3D src, IntPoint3D dest, Positioning positioning, out IntPoint3D finalLocation)
		{
			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult res1 = null;
			AStarResult res2 = null;

			var task1 = new Task(delegate
			{
				// backwards
				res1 = Find(dest, positioning, src, Positioning.Exact, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(env, l), 200000, cts.Token);
			});
			task1.Start();

			var task2 = new Task(delegate
			{
				res2 = Find(src, Positioning.Exact, dest, positioning, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(env, l), 200000, cts.Token);
			}
			);
			task2.Start();

			task1.Wait();
			//Task.WaitAny(task1, task2);

			cts.Cancel();

			Task.WaitAll(task1, task2);

			IEnumerable<Direction> dirs;
			
			if (res1.Status == AStarStatus.Found)
			{
				dirs = res1.GetPathReverse();
				finalLocation = dest + dirs.First().Reverse();
			}
			else if (res2.Status == AStarStatus.Found)
			{
				dirs = res2.GetPath();
				finalLocation = res2.LastNode.Loc;
			}
			else
			{
				dirs = null;
				finalLocation = new IntPoint3D();
			}

			return dirs;
		}





		public static AStarResult FindNearest(IntPoint3D src, Func<IntPoint3D, bool> func, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000)
		{
			return Find(src, Positioning.Exact, new AStarDelegateTarget(func), tileWeight, validDirs, maxNodeCount);
		}

		public static AStarResult Find(IntPoint3D src, Positioning srcPositioning, IntPoint3D dst, Positioning dstPositioning, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return Find(src, srcPositioning, new AStarDefaultTarget(dst, dstPositioning), tileWeight, validDirs, maxNodeCount, cancellationToken);
		}

		public static AStarResult Find(IntPoint3D src, Positioning srcPositioning, IAStarTarget target, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var state = new AStarState()
			{
				Src = src,
				SrcPositioning = srcPositioning,
				Target = target,
				GetTileWeight = tileWeight,
				GetValidDirs = validDirs,
				NodeMap = new Dictionary<IntPoint3D, AStarNode>(),
				OpenList = new BinaryHeap<AStarNode>(),
				//OpenList = new SimpleOpenList<AStar3DNode>(),
				CancellationToken = cancellationToken.HasValue ? cancellationToken.Value : CancellationToken.None,
			};

			return FindInternal(state, maxNodeCount);
		}

		static AStarResult FindInternal(AStarState state, int maxNodeCount)
		{
			//MyTrace.WriteLine("Start");

			AStarNode lastNode = null;
			AStarStatus status = AStarStatus.NotFound;

			var nodeMap = state.NodeMap;
			var openList = state.OpenList;

			IEnumerable<IntPoint3D> startLocations;

			switch (state.SrcPositioning)
			{
				case Positioning.Exact:
					startLocations = new IntPoint3D[] { state.Src };
					break;

				case Positioning.AdjacentCardinal:
					startLocations = DirectionExtensions.CardinalDirections.Select(d => state.Src + d);
					break;

				case Positioning.AdjacentPlanar:
					startLocations = DirectionExtensions.PlanarDirections.Select(d => state.Src + d);
					break;

				case Positioning.AdjacentCardinalUpDown:
					startLocations = DirectionExtensions.CardinalUpDownDirections.Select(d => state.Src + d);
					break;

				case Positioning.AdjacentPlanarUpDown:
					startLocations = DirectionExtensions.PlanarUpDownDirections.Select(d => state.Src + d);
					break;

				case Positioning.Adjacent:
					throw new NotImplementedException();

				default:
					throw new Exception();
			}

			foreach (var p in startLocations)
			{
				var node = new AStarNode(p, null);
				openList.Add(node);
				nodeMap.Add(p, node);
			}

			while (!openList.IsEmpty)
			{
				if (state.CancellationToken.IsCancellationRequested)
				{
					status = AStarStatus.Cancelled;
					break;
				}

				var node = openList.Pop();
				node.Closed = true;

				if (state.Target.GetIsTarget(node.Loc))
				{
					lastNode = node;
					Debug.Assert(node.Parent != null);
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

		static ushort CostBetweenNodes(IntPoint3D from, IntPoint3D to)
		{
			ushort cost = (from - to).ManhattanLength == 1 ? (ushort)10 : (ushort)14;
			return cost;
		}

		static void CheckNeighbors(AStarState state, AStarNode parent)
		{
			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

				AStarNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.GetTileWeight(childLoc));
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

			foreach (var dir in state.GetValidDirs(parent.Loc))
			{
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

				AStarNode child;
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
					IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

					AStarNode child;
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
	}
}
