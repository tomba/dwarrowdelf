using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Jobs
{
	/// <summary>
	/// AI that takes jobs from a JobManager
	/// </summary>
	public class JobManagerAI : AssignmentAI
	{
		JobManager m_jobManager;

		public JobManagerAI(ILiving worker, JobManager jobManager)
			: base(worker)
		{
			m_jobManager = jobManager;
		}

		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (this.CurrentAssignment != null)
				return this.CurrentAssignment;

			return m_jobManager.FindJob(this.Worker);
		}
	}
}
