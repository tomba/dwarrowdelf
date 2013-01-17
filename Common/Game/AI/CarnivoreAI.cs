using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.AI
{
	[SaveGameObject]
	public sealed class CarnivoreAI : AssignmentAI
	{
		[SaveGameProperty]
		bool m_priorityAction;

		CarnivoreAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public CarnivoreAI(ILivingObject ob, byte aiID)
			: base(ob, aiID)
		{
			m_priorityAction = false;
		}

		public override string Name { get { return "CarnivoreAI"; } }

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
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

				return new Dwarrowdelf.Jobs.Assignments.RandomMoveAssignment(null);
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
