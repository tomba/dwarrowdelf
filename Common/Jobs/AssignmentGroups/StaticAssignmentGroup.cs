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
		ObservableCollection<IAssignment> m_assignments;
		public ReadOnlyObservableCollection<IAssignment> Assignments { get; private set; }
		int m_state;

		protected StaticAssignmentGroup(IJob parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		protected void SetAssignments(IEnumerable<IAssignment> assignments)
		{
			if (m_assignments != null)
				throw new Exception();

			m_assignments = new ObservableCollection<IAssignment>(assignments);
			this.Assignments = new ReadOnlyObservableCollection<IAssignment>(m_assignments);
		}

		protected override void AssignOverride(ILiving worker)
		{
			m_state = 0;
			SetStatus(JobState.Ok);
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

			if (m_state == m_assignments.Count)
			{
				SetStatus(JobState.Done);
				return;
			}

			SetState();
		}

		void SetState()
		{
			if (m_state >= m_assignments.Count)
				throw new Exception();

			var assignment = m_assignments[m_state];
			SetAssignment(assignment);
		}
	}
}
