using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class FetchItem : StaticSerialActionJob
	{
		public FetchItem(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject item)
			: base(parent, priority)
		{
			var jobs = new IActionJob[] {
				new MoveActionJob(this, priority, item.Environment, item.Location, false),
				new GetItemActionJob(this, priority, item),
				new MoveActionJob(this, priority, env, location, false),
				new DropItemActionJob(this, priority, item),
			};

			SetSubJobs(jobs);
		}

		public override string ToString()
		{
			return "FetchItem";
		}
	}
}
