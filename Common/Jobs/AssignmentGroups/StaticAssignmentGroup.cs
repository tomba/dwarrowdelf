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

		protected override IEnumerator<IAssignment> GetAssignmentEnumerator()
		{
			return m_assignments.GetEnumerator();
		}

		protected override void OnStateChanging(JobState state)
		{
			switch (state)
			{
				case JobState.Ok:
					foreach (var job in m_assignments)
						job.Retry();
					break;

				case JobState.Done:
					break;

				case JobState.Abort:
					foreach (var job in m_assignments)
						job.Abort();
					break;

				case JobState.Fail:
					foreach (var job in m_assignments)
						job.Fail();
					break;
			}
		}

		protected override JobState CheckProgress()
		{
			if (this.Assignments.All(j => j.JobState == JobState.Done))
				return JobState.Done;
			else
				return JobState.Ok;
		}
	}
}
