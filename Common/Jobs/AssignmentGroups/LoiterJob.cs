using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class LoiterJob : AssignmentGroup
	{
		readonly IEnvironment m_environment;
		int m_state;

		public LoiterJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;
		}

		protected override void AssignOverride(ILiving worker)
		{
			SetStatus(JobState.Ok);

			m_state = 0;
			SetState();
		}

		protected override void OnAssignmentStateChanged(JobState jobState)
		{
			if (jobState == Jobs.JobState.Ok)
				return;

			if (jobState == Jobs.JobState.Fail)
			{
				SetStatus(JobState.Fail);
				return;
			}

			if (jobState == Jobs.JobState.Abort)
			{
				SetStatus(Jobs.JobState.Abort); // XXX check why the job aborted, and possibly retry
				return;
			}

			// else Done

			m_state++;

			SetState();
		}

		void SetState()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact);
					break;

				case 1:
					assignment = new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(14, 18, 9), DirectionSet.Exact);
					break;

				case 2:
					assignment = new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(14, 28, 9), DirectionSet.Exact);
					break;

				case 3:
					assignment = new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 28, 9), DirectionSet.Exact);
					break;

				case 4:
					assignment = new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact);
					break;

				case 5:
					m_state = 0;
					SetState();
					return;

				default:
					throw new Exception();
			}

			SetAssignment(assignment);
		}

		public override string ToString()
		{
			return "LoiterJob";
		}
	}
}
