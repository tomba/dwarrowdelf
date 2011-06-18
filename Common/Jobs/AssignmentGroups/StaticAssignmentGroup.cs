using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public abstract class StaticAssignmentGroup : AssignmentGroup
	{
		[SaveGameProperty]
		ObservableCollection<IAssignment> m_assignments;
		public ReadOnlyObservableCollection<IAssignment> Assignments { get; private set; }
		[SaveGameProperty]
		int m_state;

		protected StaticAssignmentGroup(IJob parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		protected StaticAssignmentGroup(SaveGameContext ctx)
			: base(ctx)
		{
			this.Assignments = new ReadOnlyObservableCollection<IAssignment>(m_assignments);
		}

		protected void SetAssignments(IEnumerable<IAssignment> assignments)
		{
			if (m_assignments != null)
				throw new Exception();

			m_assignments = new ObservableCollection<IAssignment>(assignments);
			this.Assignments = new ReadOnlyObservableCollection<IAssignment>(m_assignments);
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			m_state = 0;

			foreach (var assignment in m_assignments.Where(a => a.JobStatus != Jobs.JobStatus.Ok))
				assignment.Retry();

			return JobStatus.Ok;
		}

		protected override void OnAssignmentStateChanged(JobStatus jobState)
		{
			if (jobState == Jobs.JobStatus.Ok)
				return;

			if (jobState == Jobs.JobStatus.Fail)
			{
				SetStatus(JobStatus.Fail);
				return;
			}

			if (jobState == Jobs.JobStatus.Abort)
			{
				SetStatus(Jobs.JobStatus.Abort); // XXX check why the job aborted, and possibly retry
				return;
			}

			// else Done

			m_state++;

			if (m_state == m_assignments.Count)
			{
				SetStatus(JobStatus.Done);
				return;
			}
		}

		protected override IAssignment PrepareNextAssignment()
		{
			if (m_state >= m_assignments.Count)
				throw new Exception();

			var assignment = m_assignments[m_state];

			Debug.Assert(assignment.JobStatus == Jobs.JobStatus.Ok);

			return assignment;
		}
	}
}
