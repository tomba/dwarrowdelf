using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.AStar
{
	public static class AStarFinder
	{
		/// <summary>
		/// Find route from src to dst, using the given positionings
		/// </summary>
		public static AStarResult Find(IEnvironmentObject env, IntPoint3 src, DirectionSet srcPositioning, IntPoint3 dst, DirectionSet dstPositioning,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return Find(env, src, srcPositioning, new AStarDefaultTarget(dst, dstPositioning),
				maxNodeCount, cancellationToken);
		}

		/// <summary>
		/// Flood-find the nearest location for which func returns true
		/// </summary>
		public static AStarResult FindNearest(IEnvironmentObject env, IntPoint3 src, Func<IntPoint3, bool> func, int maxNodeCount = 200000)
		{
			return Find(env, src, DirectionSet.Exact, new AStarDelegateTarget(func), maxNodeCount);
		}

		/// <summary>
		/// Find route from src to destination defined by IAstarTarget
		/// </summary>
		public static AStarResult Find(IEnvironmentObject env, IntPoint3 src, DirectionSet srcPositioning, IAStarTarget target,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var initLocs = env.GetPositioningLocations(src, srcPositioning);

			var astar = new AStarImpl(initLocs, target, p => EnvironmentExtensions.GetDirectionsFrom(env, p), null);
			astar.MaxNodeCount = maxNodeCount;
			if (cancellationToken.HasValue)
				astar.CancellationToken = cancellationToken.Value;

			var status = astar.Find();
			return new AStarResult(astar.Nodes, astar.LastNode, status);
		}

		/* Parallel */

		/// <summary>
		/// Returns if dst can be reached from src
		/// </summary>
		public static bool CanReach(IEnvironmentObject env, IntPoint3 src, IntPoint3 dst, DirectionSet dstPositioning)
		{
			Debug.Assert(env != null);

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult resBackward = null;
			AStarResult resForward = null;

			var taskForward = new Task(delegate
			{
				resForward = Find(env, src, DirectionSet.Exact, dst, dstPositioning, 200000, cts.Token);
			});
			taskForward.Start();

			var taskBackward = new Task(delegate
			{
				resBackward = Find(env, dst, dstPositioning, src, DirectionSet.Exact, 200000, cts.Token);
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
		public static IEnumerable<Direction> Find(IEnvironmentObject env, IntPoint3 src, IntPoint3 dest, DirectionSet positioning)
		{
			AStarResult resBackward;
			AStarResult resForward;

			ParallelFind(env, src, dest, positioning, out resBackward, out resForward);

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
		public static IEnumerable<Direction> Find(IEnvironmentObject env, IntPoint3 src, IntPoint3 dest, DirectionSet positioning,
			out IntPoint3 finalLocation)
		{
			AStarResult resBackward;
			AStarResult resForward;

			ParallelFind(env, src, dest, positioning, out resBackward, out resForward);

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

		static void ParallelFind(IEnvironmentObject env, IntPoint3 src, IntPoint3 dest, DirectionSet positioning, out AStarResult resBackward, out AStarResult resForward)
		{
			Debug.Assert(env != null);

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked
			CancellationTokenSource cts = new CancellationTokenSource();

			AStarResult rb = null;
			AStarResult rf = null;

			var taskForward = new Task(delegate
			{
				rf = Find(env, src, DirectionSet.Exact, dest, positioning, 200000, cts.Token);
			});
			taskForward.Start();

			var taskBackward = new Task(delegate
			{
				rb = Find(env, dest, positioning, src, DirectionSet.Exact, 200000, cts.Token);
			});
			taskBackward.Start();

			Task.WaitAny(taskBackward, taskForward);

			cts.Cancel();

			Task.WaitAll(taskBackward, taskForward);

			resForward = rf;
			resBackward = rb;
		}

		public static IEnumerable<AStarResult> FindMany(IEnvironmentObject env,
			IntPoint3 src, DirectionSet srcPositioning, Func<IntPoint3, bool> func,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			return FindMany(env, src, srcPositioning, new AStarDelegateTarget(func), maxNodeCount, cancellationToken);
		}

		public static IEnumerable<AStarResult> FindMany(IEnvironmentObject env,
			IntPoint3 src, DirectionSet srcPositioning, IAStarTarget target,
			int maxNodeCount = 200000, CancellationToken? cancellationToken = null)
		{
			var initLocs = env.GetPositioningLocations(src, srcPositioning);

			var astar = new AStarImpl(initLocs, target, p => EnvironmentExtensions.GetDirectionsFrom(env, p), null);
			astar.MaxNodeCount = maxNodeCount;
			if (cancellationToken.HasValue)
				astar.CancellationToken = cancellationToken.Value;

			AStarStatus status;
			while ((status = astar.Find()) == AStarStatus.Found)
				yield return new AStarResult(astar.Nodes, astar.LastNode, status);
		}
	}
}
