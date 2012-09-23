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
		int GetTileWeight(IntPoint3 p);
		IEnumerable<Direction> GetValidDirs(IntPoint3 p);
		bool CanEnter(IntPoint3 p);
		// XXX Callback for single-stepping. Remove at some point.
		void Callback(IDictionary<IntPoint3, AStarNode> nodes);
	}

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

	public static class AStarFinder
	{
		public const int COST_DIAGONAL = 14;
		public const int COST_STRAIGHT = 10;

		/// <summary>
		/// Find route from src to dst, using the given positionings
		/// </summary>
		public static AStarResult Find(IAStarEnvironment environment, IntPoint3 src, DirectionSet srcPositioning, IntPoint3 dst, DirectionSet dstPositioning,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return Find(environment, src, srcPositioning, new AStarDefaultTarget(dst, dstPositioning),
				maxNodeCount, cancellationToken);
		}

		/// <summary>
		/// Flood-find the nearest location for which func returns true
		/// </summary>
		public static AStarResult FindNearest(IAStarEnvironment environment, IntPoint3 src, Func<IntPoint3, bool> func, int maxNodeCount = 200000)
		{
			return Find(environment, src, DirectionSet.Exact, new AStarDelegateTarget(func), maxNodeCount);
		}

		/// <summary>
		/// Find route from src to destination defined by IAstarTarget
		/// </summary>
		public static AStarResult Find(IAStarEnvironment environment, IntPoint3 src, DirectionSet srcPositioning, IAStarTarget target,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var astar = new AStarImpl(environment, src, srcPositioning, target, maxNodeCount, cancellationToken);
			var status = astar.Find();
			return new AStarResult(astar.Nodes, astar.LastNode, status);
		}

		/* Parallel */

		/// <summary>
		/// Returns if dst can be reached from src
		/// </summary>
		public static bool CanReach(IAStarEnvironment environment, IntPoint3 src, IntPoint3 dst, DirectionSet dstPositioning)
		{
			Debug.Assert(environment != null);

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult resBackward = null;
			AStarResult resForward = null;

			var taskForward = new Task(delegate
			{
				resForward = Find(environment, src, DirectionSet.Exact, dst, dstPositioning, 200000, cts.Token);
			});
			taskForward.Start();

			var taskBackward = new Task(delegate
			{
				resBackward = Find(environment, dst, dstPositioning, src, DirectionSet.Exact, 200000, cts.Token);
			});
			taskBackward.Start();

			Task.WaitAny(taskBackward, taskForward);

			cts.Cancel();

			Task.WaitAll(taskBackward, taskForward);

			if (resForward.Status == AStarStatus.Found || resBackward.Status == AStarStatus.Found)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Find route from src to dest, finding the route in parallel from both directions
		/// </summary>
		public static IEnumerable<Direction> Find(IAStarEnvironment environment, IntPoint3 src, IntPoint3 dest, DirectionSet positioning)
		{
			AStarResult resBackward;
			AStarResult resForward;

			ParallelFind(environment, src, dest, positioning, out resBackward, out resForward);

			IEnumerable<Direction> dirs;

			if (resForward.Status == AStarStatus.Found)
				dirs = resForward.GetPath();
			else if (resBackward.Status == AStarStatus.Found)
				dirs = resBackward.GetPathReverse();
			else
				dirs = null;

			return dirs;
		}

		/// <summary>
		/// Find route from src to dest, finding the route in parallel from both directions
		/// </summary>
		public static IEnumerable<Direction> Find(IAStarEnvironment environment, IntPoint3 src, IntPoint3 dest, DirectionSet positioning,
			out IntPoint3 finalLocation)
		{
			AStarResult resBackward;
			AStarResult resForward;

			ParallelFind(environment, src, dest, positioning, out resBackward, out resForward);

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
				finalLocation = new IntPoint3();
			}

			return dirs;
		}

		static void ParallelFind(IAStarEnvironment environment, IntPoint3 src, IntPoint3 dest, DirectionSet positioning, out AStarResult resBackward, out AStarResult resForward)
		{
			Debug.Assert(environment != null);

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult rb = null;
			AStarResult rf = null;

			var taskForward = new Task(delegate
			{
				rf = Find(environment, src, DirectionSet.Exact, dest, positioning, 200000, cts.Token);
			});
			taskForward.Start();

			var taskBackward = new Task(delegate
			{
				rb = Find(environment, dest, positioning, src, DirectionSet.Exact, 200000, cts.Token);
			});
			taskBackward.Start();

			Task.WaitAny(taskBackward, taskForward);

			cts.Cancel();

			Task.WaitAll(taskBackward, taskForward);

			resForward = rf;
			resBackward = rb;
		}

		public static IEnumerable<AStarResult> FindMany(IAStarEnvironment environment,
			IntPoint3 src, DirectionSet srcPositioning, Func<IntPoint3, bool> func,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return FindMany(environment, src, srcPositioning, new AStarDelegateTarget(func), maxNodeCount, cancellationToken);
		}

		public static IEnumerable<AStarResult> FindMany(IAStarEnvironment environment,
			IntPoint3 src, DirectionSet srcPositioning, IAStarTarget target,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var astar = new AStarImpl(environment, src, srcPositioning, target, maxNodeCount, cancellationToken);

			AStarStatus status;
			while ((status = astar.Find()) == AStarStatus.Found)
				yield return new AStarResult(astar.Nodes, astar.LastNode, status);
		}

		sealed class AStarImpl
		{
			readonly IAStarEnvironment m_environment;
			readonly IntPoint3 m_src;
			readonly DirectionSet m_srcPositioning;
			readonly CancellationToken m_cancellationToken;
			readonly int m_maxNodeCount;

			IAStarTarget m_target;
			IOpenList<AStarNode> m_openList;
			Dictionary<IntPoint3, AStarNode> m_nodeMap;

			public Dictionary<IntPoint3, AStarNode> Nodes { get { return m_nodeMap; } }
			public AStarNode LastNode { get; private set; }

			public AStarImpl(IAStarEnvironment environment, IntPoint3 src, DirectionSet srcPositioning,
				IAStarTarget target, int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
			{
				m_environment = environment;
				m_src = src;
				m_srcPositioning = srcPositioning;
				m_maxNodeCount = maxNodeCount;
				m_cancellationToken = cancellationToken.HasValue ? cancellationToken.Value : CancellationToken.None;

				m_target = target;
				m_nodeMap = new Dictionary<IntPoint3, AStarNode>();
				m_openList = new BinaryHeap<AStarNode>();

				AddInitialNodes();
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

					m_environment.Callback(nodeMap);

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

			void AddInitialNodes()
			{
				var nodeMap = m_nodeMap;
				var openList = m_openList;

				IEnumerable<IntPoint3> nodeList;

				nodeList = m_srcPositioning.ToDirections().Select(d => m_src + d);

				foreach (var p in nodeList.Where(p => m_environment.CanEnter(p)))
				{
					ushort g = 0;
					ushort h = m_target.GetHeuristic(p);

					var node = new AStarNode(p, null);
					node.G = g;
					node.H = h;
					openList.Add(node);
					nodeMap.Add(p, node);
				}
			}

			static ushort CostBetweenNodes(IntPoint3 from, IntPoint3 to)
			{
				ushort cost = (from - to).ManhattanLength == 1 ? (ushort)COST_STRAIGHT : (ushort)COST_DIAGONAL;
				return cost;
			}

			void CheckNeighbors(AStarNode parent)
			{
				foreach (var dir in m_environment.GetValidDirs(parent.Loc))
				{
					IntPoint3 childLoc = parent.Loc + new IntVector3(dir);

					AStarNode child;
					m_nodeMap.TryGetValue(childLoc, out child);
					//if (child != null && child.Closed)
					//	continue;

					ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + m_environment.GetTileWeight(childLoc));
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
				foreach (var dir in m_environment.GetValidDirs(parent.Loc))
				{
					IntPoint3 childLoc = parent.Loc + new IntVector3(dir);

					AStarNode child;
					m_nodeMap.TryGetValue(childLoc, out child);
					if (child == null)
						continue;

					ushort g = (ushort)(parent.G + CostBetweenNodes(parent.Loc, childLoc) + m_environment.GetTileWeight(childLoc));

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
}
