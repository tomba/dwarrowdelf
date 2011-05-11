using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	public abstract class MoveAssignmentBase : Assignment
	{
		protected IntPoint3D Src { get; private set; } // just for ToString()

		protected readonly IEnvironment m_environment;
		DirectionSet m_positioning;
		IntPoint3D m_supposedLocation;
		int m_numFails;
		Queue<Direction> m_pathDirs;

		public MoveAssignmentBase(IJob parent, ActionPriority priority, IEnvironment environment, DirectionSet positioning)
			: base(parent, priority)
		{
			m_environment = environment;
			m_positioning = positioning;
		}

		public DirectionSet Positioning
		{
			get { return m_positioning; }
			set
			{
				m_positioning = value;
				m_pathDirs = null;
			}
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
			this.Src = worker.Location;
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
					return CheckProgress(this.Worker);

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
			var progress = CheckProgress(worker);
			
			if (progress != Jobs.JobState.Ok)
				return progress;

			var path = GetPath(worker);

			if (path == null)
				return Jobs.JobState.Abort;

			if (path.Count == 0)
				return Jobs.JobState.Done;

			m_pathDirs = path;
			m_supposedLocation = worker.Location;

			return JobState.Ok;
		}

		protected abstract Queue<Direction> GetPath(ILiving worker);

		protected abstract JobState CheckProgress(ILiving worker);
	}
}
