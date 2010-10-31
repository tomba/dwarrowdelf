using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.AStar
{
	public enum AStarStatus
	{
		Found,
		NotFound,
		LimitExceeded,
		Cancelled,
	}

	public class AStar3DResult
	{
		public IDictionary<IntPoint3D, AStar3DNode> Nodes { get; private set; }
		public AStar3DNode LastNode { get; private set; }
		public AStarStatus Status { get; private set; }

		internal AStar3DResult(IDictionary<IntPoint3D, AStar3DNode> nodes, AStar3DNode lastNode, AStarStatus status)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			this.Nodes = nodes;
			this.LastNode = lastNode;
			this.Status = status;
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
			return GetPathReverse().Reverse().Select(d => d.Reverse());
		}
	}

	// tries to save some memory by using ushorts.
	public class AStar3DNode : IAStarNode
	{
		public IntPoint3D Loc { get; private set; }
		public AStar3DNode Parent;
		public ushort G { get; set; }
		public ushort H { get; set; }
		public ushort F { get { return (ushort)(G + H); } }
		public bool Closed { get; set; }

		public AStar3DNode(IntPoint3D l, AStar3DNode parent)
		{
			Loc = l;
			Parent = parent;
		}
	}

	public interface IAStarTarget
	{
		bool GetIsTarget(IntPoint3D location);
		ushort GetHeuristic(IntPoint3D location);
	}

	public class AStarDefaultTarget : IAStarTarget
	{
		IntPoint3D m_destination;
		Positioning m_positioning;

		public AStarDefaultTarget(IntPoint3D destination, Positioning positioning)
		{
			m_destination = destination;
			m_positioning = positioning;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return location.IsAdjacentTo(m_destination, m_positioning);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			return (ushort)((m_destination - location).ManhattanLength * 10);
		}
	}

	public class AStarAreaTarget : IAStarTarget
	{
		IntCuboid m_destination;

		public AStarAreaTarget(IntCuboid destination)
		{
			m_destination = destination;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return m_destination.Contains(location);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			var dst = new IntPoint3D((m_destination.X1 + m_destination.X2) / 2, (m_destination.Y1 + m_destination.Y2) / 2, (m_destination.Z1 + m_destination.Z2) / 2);
			return (ushort)((dst - location).ManhattanLength * 10);
		}
	}

	public class AStarDelegateTarget : Dwarrowdelf.AStar.IAStarTarget
	{
		Func<IntPoint3D, bool> m_func;

		public AStarDelegateTarget(Func<IntPoint3D, bool> func)
		{
			m_func = func;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return m_func(location);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			return 0;
		}
	}

	public static class AStar3D
	{
		class AStarState
		{
			public IAStarTarget Target;
			public IntPoint3D Src;
			public Positioning SrcPositioning;
			public IOpenList<AStar3DNode> OpenList;
			public IDictionary<IntPoint3D, AStar3DNode> NodeMap;
			public Func<IntPoint3D, int> GetTileWeight;
			public Func<IntPoint3D, IEnumerable<Direction>> GetValidDirs;
			public CancellationToken CancellationToken;
		}


		public static IEnumerable<Direction> Find(IEnvironment env, IntPoint3D src, IntPoint3D dest, Positioning positioning, out IntPoint3D finalLocation)
		{
			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStar.AStar3DResult res1 = null;
			AStar.AStar3DResult res2 = null;

			var task1 = new Task(delegate
			{
				// backwards
				res1 = AStar.AStar3D.Find(dest, positioning, src, Positioning.Exact, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(env, l), 200000, cts.Token);
			});
			task1.Start();

			var task2 = new Task(delegate
			{
				res2 = AStar.AStar3D.Find(src, Positioning.Exact, dest, positioning, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(env, l), 200000, cts.Token);
			}
			);
			task2.Start();

			task1.Wait();
			//Task.WaitAny(task1, task2);

			cts.Cancel();

			Task.WaitAll(task1, task2);

			IEnumerable<Direction> dirs;
			
			if (res1.Status == AStar.AStarStatus.Found)
			{
				dirs = res1.GetPathReverse();
				finalLocation = dest + dirs.First().Reverse();
			}
			else if (res2.Status == AStar.AStarStatus.Found)
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





		public static AStar3DResult FindNearest(IntPoint3D src, Func<IntPoint3D, bool> func, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000)
		{
			return Find(src, Positioning.Exact, new AStarDelegateTarget(func), tileWeight, validDirs, maxNodeCount);
		}

		public static AStar3DResult Find(IntPoint3D src, Positioning srcPositioning, IntPoint3D dst, Positioning dstPositioning, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return Find(src, srcPositioning, new AStarDefaultTarget(dst, dstPositioning), tileWeight, validDirs, maxNodeCount, cancellationToken);
		}

		public static AStar3DResult Find(IntPoint3D src, Positioning srcPositioning, IAStarTarget target, Func<IntPoint3D, int> tileWeight,
			Func<IntPoint3D, IEnumerable<Direction>> validDirs, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var state = new AStarState()
			{
				Src = src,
				SrcPositioning = srcPositioning,
				Target = target,
				GetTileWeight = tileWeight,
				GetValidDirs = validDirs,
				NodeMap = new Dictionary<IntPoint3D, AStar3DNode>(),
				OpenList = new BinaryHeap<AStar3DNode>(),
				//OpenList = new SimpleOpenList<AStar3DNode>(),
				CancellationToken = cancellationToken.HasValue ? cancellationToken.Value : CancellationToken.None,
			};

			return FindInternal(state, maxNodeCount);
		}

		static AStar3DResult FindInternal(AStarState state, int maxNodeCount)
		{
			//MyTrace.WriteLine("Start");

			AStar3DNode lastNode = null;
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
				var node = new AStar3DNode(p, null);
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

			return new AStar3DResult(nodeMap, lastNode, status);
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
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

				AStar3DNode child;
				state.NodeMap.TryGetValue(childLoc, out child);
				//if (child != null && child.Closed)
				//	continue;

				ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + state.GetTileWeight(childLoc));
				ushort h = state.Target.GetHeuristic(childLoc);

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
				IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

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
					IntPoint3D childLoc = parent.Loc + new IntVector3D(dir);

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
	}
}
