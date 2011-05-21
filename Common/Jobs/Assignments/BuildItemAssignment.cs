using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class BuildItemAssignment : Assignment
	{
		[GameProperty]
		readonly IItemObject[] m_items;
		[GameProperty]
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

		protected override JobStatus ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobStatus.Ok;

				case ActionState.Done:
					return JobStatus.Done;

				case ActionState.Fail:
					return JobStatus.Fail;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			return "BuildItemAssignment";
		}
	}
}
