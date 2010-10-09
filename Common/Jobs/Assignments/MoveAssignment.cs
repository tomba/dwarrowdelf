using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

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

		protected override void OnStateChanged(JobState state)
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

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked

			CancellationTokenSource cts = new CancellationTokenSource();

			AStar.AStar3DResult res1 = null;
			AStar.AStar3DResult res2 = null;

			var task1 = new Task(delegate
			{
				res1 = AStar.AStar3D.Find(m_dest, worker.Location, true, l => 0, m_environment.GetDirectionsFrom, 200000, cts.Token);
			});
			task1.Start();

			var task2 = new Task(delegate
			{
				res2 = AStar.AStar3D.Find(worker.Location, m_dest, !m_adjacent, l => 0, m_environment.GetDirectionsFrom, 200000, cts.Token);
			}
			);
			task2.Start();

			Task.WaitAny(task1, task2);

			cts.Cancel();

			Task.WaitAll(task1, task2);

			IEnumerable<Direction> dirs;

			if (res1.Status == AStar.AStarStatus.Found)
			{
				dirs = res1.GetPathReverse();

				if (m_adjacent)
					dirs = dirs.Take(dirs.Count() - 1);
			}
			else if (res2.Status == AStar.AStarStatus.Found)
			{
				dirs = res2.GetPath();
			}
			else
				dirs = null;

			if (dirs == null)
				return Jobs.JobState.Abort;

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
