using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class MoveAssignment : Assignment
	{
		IntPoint3D m_src; // just for ToString()

		Queue<Direction> m_pathDirs;
		readonly IEnvironment m_environment;
		readonly IntPoint3D m_dest;
		readonly bool m_adjacent;
		IntPoint3D m_supposedLocation;
		int m_numFails;

		public MoveAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D destination, bool adjacent)
			: base(parent, priority)
		{
			m_environment = environment;
			m_dest = destination;
			m_adjacent = adjacent;
		}

		protected override void OnStateChanging(JobState state)
		{
			if (state == JobState.Ok)
				return;

			// else Abort, Done or Fail
			m_pathDirs = null;
			m_numFails = 0;
		}

		protected override JobState AssignOverride(ILiving worker)
		{
			m_src = worker.Location;
			m_numFails = 0;

			var res = PreparePath(worker);

			return res;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var res = PreparePath(this.Worker);

				if (res != JobState.Ok)
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
			progress = JobState.Ok;
			return action;
		}

		protected override JobState ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobState.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					m_numFails++;
					if (m_numFails > 10)
						return JobState.Abort;

					var res = PreparePath(this.Worker);
					return res;

				case ActionState.Abort:
					return JobState.Abort;

				default:
					throw new Exception();
			}
		}

		JobState PreparePath(ILiving worker)
		{
			var v = m_dest - worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
			{
				m_pathDirs = null;
				return JobState.Done;
			}

			// First try pathfinding from the destination to source with small limit. We expect it to fail with LimitExceeded,
			// but if it fails with NotFound, it means that the destination is surrounded by non-passable tiles
			// (what about one-way tiles, if such exist?)
			var backwardRes = AStar.AStar3D.Find(m_dest, worker.Location, false, l => 0, m_environment.GetDirectionsFrom, 64);
			if (backwardRes.Status == AStar.AStarStatus.NotFound)
				return Jobs.JobState.Abort;

			var res = AStar.AStar3D.Find(worker.Location, m_dest, !m_adjacent, l => 0, m_environment.GetDirectionsFrom);

			if (res.Status != AStar.AStarStatus.Found)
				return Jobs.JobState.Abort;

			var dirs = res.GetPath();

			m_pathDirs = new Queue<Direction>(dirs);

			m_supposedLocation = worker.Location;

			return JobState.Ok;
		}

		JobState CheckProgress()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return JobState.Done;
			else
				return JobState.Ok;
		}

		public override string ToString()
		{
			return String.Format("Move({0} -> {1})", m_src, m_dest);
		}

	}
}
