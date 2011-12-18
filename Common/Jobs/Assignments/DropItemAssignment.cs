using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public class DropItemAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IItemObject m_item;

		public DropItemAssignment(IJobObserver parent, IItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		protected DropItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new DropAction(m_item);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "DropItemAssignment";
		}
	}
}
