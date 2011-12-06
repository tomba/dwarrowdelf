using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Dwarrowdelf;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.AI
{
	[SaveGameObjectByRef]
	public class HerbivoreAI : AssignmentAI
	{
		[SaveGameProperty]
		bool m_priorityAction;

		[SaveGameProperty]
		Herd m_herd;

		public Herd Herd {
			get { return m_herd; }

			set
			{
				if (m_herd != null)
					m_herd.RemoveMember(this);

				m_herd = value;

				if (m_herd != null)
					m_herd.AddMember(this);
			}
		}

		HerbivoreAI(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public HerbivoreAI(ILivingObject ob)
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

				return new Dwarrowdelf.Jobs.Assignments.GrazeMoveAssignment(null, this.Herd);
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
