using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class FetchItem : SerialActionJob
	{
		public FetchItem(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject item)
			: base(parent, priority)
		{
			AddSubJob(new MoveActionJob(this, priority, item.Environment, item.Location, false));
			AddSubJob(new GetItemActionJob(this, priority, item));
			AddSubJob(new MoveActionJob(this, priority, env, location, false));
			AddSubJob(new DropItemActionJob(this, priority, item));
		}

		public override string ToString()
		{
			return "FetchItem";
		}
	}
}
