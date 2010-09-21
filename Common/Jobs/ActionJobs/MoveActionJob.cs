using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.ActionJobs
{
	public class MoveActionJob : ActionJob
	{
		IntPoint3D m_src; // just for ToString()

		Queue<Direction> m_pathDirs;
		readonly IEnvironment m_environment;
		readonly IntPoint3D m_dest;
		readonly bool m_adjacent;
		IntPoint3D m_supposedLocation;
		int m_numFails;

		public MoveActionJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D destination, bool adjacent)
			: base(parent, priority)
		{
			m_environment = environment;
			m_dest = destination;
			m_adjacent = adjacent;
		}

		protected override void Cleanup()
		{
			m_pathDirs = null;
		}

		protected override void AbortOverride()
		{
			m_pathDirs = null;
			m_numFails = 0;
		}

		protected override Progress AssignOverride(ILiving worker)
		{
			m_src = worker.Location;

			m_numFails = 0;
			return Progress.Ok;
		}

		protected override GameAction PrepareNextActionOverride(out Progress progress)
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var res = PreparePath();

				if (res != Progress.Ok)
				{
					progress = res;
					return null;
				}
			}

			Direction dir = m_pathDirs.Dequeue();

			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			m_supposedLocation += new IntVector3D(dir);

			var action = new MoveAction(dir, this.Priority);
			progress = Progress.Ok;
			return action;
		}

		protected override Progress ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return Progress.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					m_numFails++;
					if (m_numFails > 10)
						return Progress.Fail;

					var res = PreparePath();
					return res;

				case ActionState.Abort:
					return Progress.Abort;

				default:
					throw new Exception();
			}
		}

		Progress PreparePath()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
			{
				m_pathDirs = null;
				return Progress.Done;
			}

			// First try pathfinding from the destination to source with small limit. We expect it to fail with LimitExceeded,
			// but if it fails with NotFound, it means that the destination is surrounded by non-passable tiles
			// (what about one-way tiles, if such exist?)
			var backwardRes = AStar.AStar3D.Find(m_dest, this.Worker.Location, false, l => 0, m_environment.GetDirectionsFrom, 64);
			if (backwardRes.Status == AStar.AStarStatus.NotFound)
				return Jobs.Progress.Fail;

			var res = AStar.AStar3D.Find(this.Worker.Location, m_dest, !m_adjacent, l => 0, m_environment.GetDirectionsFrom);

			if (res.Status != AStar.AStarStatus.Found)
				return Jobs.Progress.Fail;

			var dirs = res.GetPath();

			m_pathDirs = new Queue<Direction>(dirs);

			m_supposedLocation = this.Worker.Location;

			return Progress.Ok;
		}

		Progress CheckProgress()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return String.Format("Move({0} -> {1})", m_src, m_dest);
		}

	}
}
