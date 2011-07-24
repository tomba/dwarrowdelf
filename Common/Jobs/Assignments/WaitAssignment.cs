using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class WaitAssignment : Assignment
	{
		[SaveGameProperty("Turns")]
		readonly int m_turns;

		public WaitAssignment(IJob parent, ActionPriority priority, int turns)
			: base(parent, priority)
		{
			m_turns = turns;
		}

		protected WaitAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new WaitAction(m_turns, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "WaitAssignment";
		}
	}
}
