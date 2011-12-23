using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public sealed class BuildItemAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IItemObject[] m_items;
		[SaveGameProperty]
		readonly ItemID m_dstItemID;

		public BuildItemAssignment(IJobObserver parent, IItemObject[] items, ItemID dstItemID)
			: base(parent)
		{
			m_items = items;
			m_dstItemID = dstItemID;
		}

		BuildItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new BuildItemAction(m_items, m_dstItemID);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "BuildItemAssignment";
		}
	}
}
