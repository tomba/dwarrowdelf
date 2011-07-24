using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class BuildItemAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IItemObject[] m_items;
		[SaveGameProperty]
		readonly ItemID m_dstItemID;

		public BuildItemAssignment(IJob parent, ActionPriority priority, IItemObject[] items, ItemID dstItemID)
			: base(parent, priority)
		{
			m_items = items;
			m_dstItemID = dstItemID;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new BuildItemAction(m_items, m_dstItemID, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "BuildItemAssignment";
		}
	}
}
