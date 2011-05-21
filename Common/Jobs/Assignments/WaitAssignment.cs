using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class WaitAssignment : Assignment
	{
		[GameProperty("Turns")]
		readonly int m_turns;

		public WaitAssignment(IJob parent, ActionPriority priority, int turns)
			: base(parent, priority)
		{
			m_turns = turns;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new WaitAction(m_turns, this.Priority);
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
			return "WaitAssignment";
		}
	}
}
