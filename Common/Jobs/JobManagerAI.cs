using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Jobs
{
	/// <summary>
	/// AI that takes jobs from a JobManager
	/// </summary>
	[SaveGameObject]
	public class JobManagerAI : AssignmentAI
	{
		public JobManager JobManager { get; set; }

		public JobManagerAI(ILiving worker)
			: base(worker)
		{
		}

		JobManagerAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (this.CurrentAssignment != null)
				return this.CurrentAssignment;

			return this.JobManager.FindJob(this.Worker);
		}
	}
}
