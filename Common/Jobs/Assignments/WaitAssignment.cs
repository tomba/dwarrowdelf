using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class WaitAssignment : Assignment
	{
		readonly int m_turns;

		public WaitAssignment(IJob parent, ActionPriority priority, int turns)
			: base(parent, priority)
		{
			m_turns = turns;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			var action = new WaitAction(m_turns, this.Priority);
			progress = JobState.Ok;
			return action;
		}

		protected override JobState ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobState.Ok;

				case ActionState.Done:
					return JobState.Done;

				case ActionState.Fail:
					return JobState.Fail;

				case ActionState.Abort:
					return JobState.Abort;

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			return "WaitAssignment";
		}
	}
}
