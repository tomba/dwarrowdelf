﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class FetchItems : ParallelJobGroup
	{
		public FetchItems(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject[] items)
			: base(parent, priority)
		{
			foreach (var item in items)
			{
				AddSubJob(new AssignmentGroups.FetchItem(this, priority, env, location, item));
			}
		}

		public override string ToString()
		{
			return "FetchItems";
		}
	}
}