using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject(UseRef = true)]
	public class LoiterJob : AssignmentGroup
	{
		[SaveGameProperty("Environment")]
		readonly IEnvironment m_environment;
		[SaveGameProperty("State")]
		int m_state;

		public LoiterJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;
		}

		LoiterJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			m_state = 0;
			return JobStatus.Ok;
		}

		protected override void OnAssignmentStateChanged(JobStatus jobState)
		{
			switch (jobState)
			{
				case Jobs.JobStatus.Ok:
					break;

				case Jobs.JobStatus.Fail:
					SetStatus(JobStatus.Fail);
					break;

				case Jobs.JobStatus.Abort:
					SetStatus(Jobs.JobStatus.Abort); // XXX check why the job aborted, and possibly retry
					break;

				case Jobs.JobStatus.Done:
					m_state = (m_state + 1) % 5;
					break;
			}
		}

		protected override IAssignment PrepareNextAssignment()
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

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "LoiterJob";
		}
	}
}
