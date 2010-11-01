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
	public delegate IntPoint3D GetMoveTarget(out bool ok);

	public class MoveAssignment : Assignment
	{
		IntPoint3D m_src; // just for ToString()

		Queue<Direction> m_pathDirs;
		readonly IEnvironment m_environment;
		IntPoint3D m_dest;
		readonly GetMoveTarget m_destFunc;
		readonly Positioning m_positioning;
		IntPoint3D m_supposedLocation;
		int m_numFails;

		public MoveAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D destination, Positioning positioning)
			: base(parent, priority)
		{
			m_environment = environment;
			m_dest = destination;
			m_positioning = positioning;
		}

		public MoveAssignment(IJob parent, ActionPriority priority, IEnvironment environment, GetMoveTarget destination, Positioning positioning)
			: base(parent, priority)
		{
			m_environment = environment;
			m_destFunc = destination;
			m_positioning = positioning;
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

			if (m_destFunc != null)
			{
				bool ok;
				m_dest = m_destFunc(out ok);
				if (!ok)
					return Jobs.JobState.Abort;
			}

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
			if (worker.Location.IsAdjacentTo(m_dest, m_positioning))
			{
				m_pathDirs = null;
				return JobState.Done;
			}

			// Do pathfinding to both directions simultaneously to detect faster if the destination is blocked

			CancellationTokenSource cts = new CancellationTokenSource();

			AStar.AStarResult res1 = null;
			AStar.AStarResult res2 = null;

			var task1 = new Task(delegate
			{
				res1 = AStar.AStar.Find(m_dest, m_positioning, worker.Location, Positioning.Exact, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(m_environment, l), 200000, cts.Token);
			});
			task1.Start();

			var task2 = new Task(delegate
			{
				res2 = AStar.AStar.Find(worker.Location, Positioning.Exact, m_dest, m_positioning, l => 0,
					l => EnvironmentHelpers.GetDirectionsFrom(m_environment, l), 200000, cts.Token);
			}
			);
			task2.Start();

			Task.WaitAny(task1, task2);

			cts.Cancel();

			Task.WaitAll(task1, task2);

			IEnumerable<Direction> dirs;

			if (res1.Status == AStar.AStarStatus.Found)
				dirs = res1.GetPathReverse();
			else if (res2.Status == AStar.AStarStatus.Found)
				dirs = res2.GetPath();
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
			if (this.Worker.Location.IsAdjacentTo(m_dest, m_positioning))
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
