﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class DropItemAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IItemObject m_item;

		public DropItemAssignment(IJob parent, ActionPriority priority, IItemObject item)
			: base(parent, priority)
		{
			m_item = item;
		}

		protected DropItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new DropAction(new IItemObject[] { m_item }, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "DropItemAssignment";
		}
	}
}
