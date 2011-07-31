﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.AI
{
	[SaveGameObject(UseRef = true)]
	public class MonsterAI : AssignmentAI
	{
		[SaveGameProperty]
		bool m_priorityAction;

		MonsterAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public MonsterAI(ILiving ob)
			: base(ob)
		{
			m_priorityAction = false;
		}

		// return new or current assignment, or null to cancel current assignment, or do nothing is no current assignment
		protected override IAssignment GetNewOrCurrentAssignment(ActionPriority priority)
		{
			var worker = this.Worker;

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

				return new Dwarrowdelf.Jobs.Assignments.RandomMoveAssignment(null, priority);
			}
			else
			{
				throw new Exception();
			}
		}
	}
}