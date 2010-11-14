using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class AttackAssignment : Assignment
	{
		readonly IEnvironment m_environment;
		readonly ILiving m_target;

		Queue<Direction> m_pathDirs;
		IntPoint3D m_supposedLocation;
		IntPoint3D m_dest;

		public AttackAssignment(IJob parent, ActionPriority priority, IEnvironment environment, ILiving target)
			: base(parent, priority)
		{
			m_environment = environment;
			m_target = target;
		}

		protected override void OnStateChanged(JobState state)
		{
			if (state == JobState.Ok)
				return;

			// else Abort, Done or Fail
			m_pathDirs = null;
		}

		protected override JobState AssignOverride(ILiving worker)
		{
			var res = PreparePath(worker);
			if (res == Jobs.JobState.Done)
				res = Jobs.JobState.Ok;
			return res;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			if (this.Worker.Location.IsAdjacentTo(m_target.Location, DirectionSet.Planar))
			{
				var action = new AttackAction(m_target, this.Priority);
				progress = JobState.Ok;
				return action;
			}
			else
			{
				if (m_pathDirs == null || m_supposedLocation != this.Worker.Location || m_dest != m_target.Location)
				{
					var res = PreparePath(this.Worker);

					if (res != JobState.Ok)
					{
						Debug.Assert(res != Jobs.JobState.Done);
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
			m_dest = m_target.Location;

			if (worker.Location.IsAdjacentTo(m_dest, DirectionSet.Planar))
			{
				m_pathDirs = null;
				return JobState.Done;
			}

			IntPoint3D finalPos;
			var path = AStar.AStar.Find(m_environment, worker.Location, m_dest, DirectionSet.Planar, out finalPos);

			if (path == null)
				return Jobs.JobState.Abort;

			m_pathDirs = new Queue<Direction>(path);

			if (m_pathDirs.Count == 0)
				return Jobs.JobState.Done;

			m_supposedLocation = worker.Location;

			return JobState.Ok;
		}

		JobState CheckProgress()
		{
			if (m_target.IsDestructed)
				return JobState.Done;
			else
				return JobState.Ok;
		}

		public override string ToString()
		{
			return String.Format("Attach({0})", m_target);
		}

	}
}
