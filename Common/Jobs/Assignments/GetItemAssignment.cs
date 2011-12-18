using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public class GetItemAssignment : Assignment
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;

		public GetItemAssignment(IJobObserver parent, IItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		protected GetItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new GetAction(m_item);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "GetItemAssignment";
		}
	}
}
