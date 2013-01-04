using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class InstallItemAssignment : Assignment
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;
		[SaveGameProperty]
		InstallMode m_mode;

		public InstallItemAssignment(IJobObserver parent, IItemObject item, InstallMode mode)
			: base(parent)
		{
			m_item = item;
			m_mode = mode;
		}

		InstallItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new InstallItemAction(m_item, m_mode);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "InstallItemAssignment";
		}
	}
}
