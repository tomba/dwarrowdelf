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
		readonly IItemObject m_workbench;
		[SaveGameProperty]
		readonly IItemObject[] m_items;
		[SaveGameProperty]
		readonly string m_buildableItemKey;

		public BuildItemAssignment(IJobObserver parent, IItemObject workbench, string buildableItemKey, IItemObject[] items)
			: base(parent)
		{
			m_workbench = workbench;
			m_items = items;
			m_buildableItemKey = buildableItemKey;
		}

		BuildItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new BuildItemAction(m_workbench, m_buildableItemKey, m_items);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "BuildItemAssignment";
		}
	}
}
