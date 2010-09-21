using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.ActionJobs
{
	public class DropItemActionJob : ActionJob
	{
		readonly IItemObject m_item;

		public DropItemActionJob(IJob parent, ActionPriority priority, IItemObject item)
			: base(parent, priority)
		{
			m_item = item;
		}

		protected override GameAction PrepareNextActionOverride(out Progress progress)
		{
			var action = new DropAction(new IItemObject[] { m_item }, this.Priority);
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
			return "DropItemActionJob";
		}
	}
}
