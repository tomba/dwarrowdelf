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
		[SaveGameProperty("Turns")]
		readonly int m_turns;

		public SleepAssignment(IJobObserver parent, IItemObject bed, int turns)
			: base(parent)
		{
			m_bed = bed;
			m_turns = turns;
		}

		SleepAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new SleepAction(m_bed, m_turns);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "SleepAssignment";
		}
	}
}
