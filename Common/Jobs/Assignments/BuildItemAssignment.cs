using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class BuildItemAssignment : Assignment
	{
		readonly IItemObject[] m_items;

		public BuildItemAssignment(IJob parent, ActionPriority priority, IItemObject[] items)
			: base(parent, priority)
		{
			m_items = items;
		}

		protected override GameAction PrepareNextActionOverride(out Progress progress)
		{
			var action = new BuildItemAction(m_items, this.Priority);
			progress = Progress.Ok;
			return action;
		}

		protected override Progress ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return Progress.Ok;

				case ActionState.Done:
					return Progress.Done;

				case ActionState.Fail:
					return Progress.Fail;

				case ActionState.Abort:
					return Progress.Abort;

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
