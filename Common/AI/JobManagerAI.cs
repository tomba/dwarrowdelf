using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.AI
{
	/// <summary>
	/// AI that takes jobs from a JobManager
	/// </summary>
	[SaveGameObjectByRef]
	public class JobManagerAI : AssignmentAI
	{
		public JobManager JobManager { get; set; }

		public JobManagerAI(ILivingObject worker)
			: base(worker)
		{
		}

		JobManagerAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public override string Name { get { return "JobManagerAI"; } }

		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			if (this.CurrentAssignment != null)
				return this.CurrentAssignment;

			return this.JobManager.FindAssignment(this.Worker);
		}
	}
}
