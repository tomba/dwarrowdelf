using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class FetchItems : JobGroup
	{
		public FetchItems(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject[] items)
			: base(parent, priority)
		{
			foreach (var item in items)
			{
				var job = new AssignmentGroups.FetchItemAssignment(this, priority, env, location, item);
				AddSubJob(job);
			}
		}

		protected override void OnSubJobDone(IJob job)
		{
			this.RemoveSubJob(job);

			if (this.SubJobs.Count == 0)
				SetStatus(JobStatus.Done);
		}

		public override string ToString()
		{
			return "FetchItems";
		}
	}
}
