using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class ConsumeItemAssignment : Assignment
	{
		readonly IItemObject m_item;

		public ConsumeItemAssignment(IJob parent, ActionPriority priority, IItemObject item)
			: base(parent, priority)
		{
			m_item = item;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			var action = new ConsumeAction(m_item, this.Priority);
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
			return "ConsumeItemAssignment";
		}
	}
}
