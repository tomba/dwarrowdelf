using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public sealed class AttackAssignment : Assignment
	{
		[SaveGameProperty("Target")]
		readonly ILivingObject m_target;

		[SaveGameProperty]
		Queue<Direction> m_pathDirs;
		[SaveGameProperty]
		IntPoint3 m_supposedLocation;
		[SaveGameProperty]
		IntPoint3 m_dest;

		public AttackAssignment(IJobObserver parent, ILivingObject target)
			: base(parent)
		{
			m_target = target;
		}

		AttackAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
			m_pathDirs = null;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			if (m_target.IsDestructed)
			{
				progress = JobStatus.Done;
				return null;
			}

			if (this.Worker.Location.IsAdjacentTo(m_target.Location, DirectionSet.Planar))
			{
				var action = new AttackAction(m_target);
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
						Debug.Assert(res != JobStatus.Done);
						progress = res;
						return null;
					}
				}

				Direction dir = m_pathDirs.Dequeue();

				if (m_pathDirs.Count == 0)
					m_pathDirs = null;

				m_supposedLocation += new IntVector3(dir);

				var action = new MoveAction(dir);
				progress = JobStatus.Ok;
				return action;
			}
		}

		protected override JobStatus ActionProgressOverride()
		{
			if (this.CurrentAction is MoveAction && this.Worker.Location.IsAdjacentTo(m_target.Location, DirectionSet.Planar))
				return JobStatus.Abort;

			return JobStatus.Ok;
		}

		protected override JobStatus ActionDoneOverride(ActionState actionStatus)
		{
			if (CheckProgress() == JobStatus.Done)
				return JobStatus.Done;

			switch (actionStatus)
			{
				case ActionState.Done:
					return JobStatus.Ok;

				case ActionState.Fail:
					var res = PreparePath(this.Worker);
					return res;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		JobStatus PreparePath(ILivingObject worker)
		{
			m_dest = m_target.Location;

			if (worker.Location.IsAdjacentTo(m_dest, DirectionSet.Planar))
			{
				m_pathDirs = null;
				return JobStatus.Done;
			}

			IntPoint3 finalPos;
			var path = AStar.AStarFinder.Find(m_target.Environment, worker.Location, m_dest, DirectionSet.Planar, out finalPos);

			if (path == null)
				return JobStatus.Abort;

			m_pathDirs = new Queue<Direction>(path);

			if (m_pathDirs.Count == 0)
				return JobStatus.Done;

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
