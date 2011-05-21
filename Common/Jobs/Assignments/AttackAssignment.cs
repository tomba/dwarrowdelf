using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class AttackAssignment : Assignment
	{
		[GameProperty("Environment")]
		readonly IEnvironment m_environment;
		[GameProperty("Target")]
		readonly ILiving m_target;

		[GameProperty]
		Queue<Direction> m_pathDirs;
		[GameProperty]
		IntPoint3D m_supposedLocation;
		[GameProperty]
		IntPoint3D m_dest;

		public AttackAssignment(IJob parent, ActionPriority priority, IEnvironment environment, ILiving target)
			: base(parent, priority)
		{
			m_environment = environment;
			m_target = target;
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
			m_pathDirs = null;
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			var res = PreparePath(worker);
			if (res == Jobs.JobStatus.Done)
				res = Jobs.JobStatus.Ok;
			return res;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			if (this.Worker.Location.IsAdjacentTo(m_target.Location, DirectionSet.Planar))
			{
				var action = new AttackAction(m_target, this.Priority);
				progress = JobStatus.Ok;
				return action;
			}
			else
			{
				if (m_pathDirs == null || m_supposedLocation != this.Worker.Location || m_dest != m_target.Location)
				{
					var res = PreparePath(this.Worker);

					if (res != JobStatus.Ok)
					{
						Debug.Assert(res != Jobs.JobStatus.Done);
						progress = res;
						return null;
					}
				}

				Direction dir = m_pathDirs.Dequeue();

				if (m_pathDirs.Count == 0)
					m_pathDirs = null;

				m_supposedLocation += new IntVector3D(dir);

				var action = new MoveAction(dir, this.Priority);
				progress = JobStatus.Ok;
				return action;
			}
		}

		protected override JobStatus ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobStatus.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					var res = PreparePath(this.Worker);
					return res;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		JobStatus PreparePath(ILiving worker)
		{
			m_dest = m_target.Location;

			if (worker.Location.IsAdjacentTo(m_dest, DirectionSet.Planar))
			{
				m_pathDirs = null;
				return JobStatus.Done;
			}

			IntPoint3D finalPos;
			var path = AStar.AStar.Find(m_environment, worker.Location, m_dest, DirectionSet.Planar, out finalPos);

			if (path == null)
				return Jobs.JobStatus.Abort;

			m_pathDirs = new Queue<Direction>(path);

			if (m_pathDirs.Count == 0)
				return Jobs.JobStatus.Done;

			m_supposedLocation = worker.Location;

			return JobStatus.Ok;
		}

		JobStatus CheckProgress()
		{
			if (m_target.IsDestructed)
				return JobStatus.Done;
			else
				return JobStatus.Ok;
		}

		public override string ToString()
		{
			return String.Format("Attach({0})", m_target);
		}

	}
}
