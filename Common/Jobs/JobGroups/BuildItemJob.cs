using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class BuildItemJob : SerialJobGroup
	{
		public BuildItemJob(IBuildingObject workplace, ActionPriority priority, IItemObject[] sourceObjects, ItemID dstItemID)
			: base(null, priority)
		{
			var env = workplace.Environment;
			var location = workplace.Area.Center;

			var jobs = new IJob[] {
				new FetchItems(this, priority, env, location, sourceObjects),
				new AssignmentGroups.BuildItem(this, priority, workplace, sourceObjects, dstItemID),
			};

			SetSubJobs(jobs);
		}

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}
}
