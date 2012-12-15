using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public sealed class SleepAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IItemObject m_bed;

		public SleepAssignment(IJobObserver parent, IItemObject bed)
			: base(parent)
		{
			m_bed = bed;
		}

		SleepAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new SleepAction(m_bed);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "SleepAssignment";
		}
	}
}
