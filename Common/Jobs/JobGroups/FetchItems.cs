using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObjectByRef]
	public class FetchItems : JobGroup
	{
		public FetchItems(IJobObserver parent, IEnvironment env, IntPoint3D location, IEnumerable<IItemObject> items)
			: base(parent)
		{
			foreach (var item in items)
			{
				var job = new AssignmentGroups.FetchItemAssignment(this, env, location, item);
				AddSubJob(job);
			}

			Debug.Assert(this.SubJobs.Count > 0);
		}

		protected FetchItems(SaveGameContext ctx)
			: base(ctx)
		{
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
