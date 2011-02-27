using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Server
{
	[GameObject]
	public class AnimalAI : AssignmentAI
	{
		[GameProperty]
		bool m_priorityAction;

		AnimalAI()
		{
		}

		public AnimalAI(Living ob)
			: base(ob)
		{
			m_priorityAction = false;
		}

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			var worker = (Living)this.Worker;

			bool hasAssignment = this.CurrentAssignment != null;
			bool hasOtherAssignment = this.CurrentAssignment == null && this.Worker.HasAction;

			if (priority == ActionPriority.High)
			{
				if (m_priorityAction)
					return this.CurrentAssignment;

				return this.CurrentAssignment;
			}
			else if (priority == ActionPriority.Idle)
			{
				if (hasOtherAssignment)
					return null;

				if (m_priorityAction)
					return this.CurrentAssignment;

				if (hasAssignment)
					return this.CurrentAssignment;

				return new Jobs.Assignments.RandomMoveAssignment(null, priority, worker.Environment);
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
