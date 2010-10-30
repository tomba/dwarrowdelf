using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class MoveConsumeJob : StaticAssignmentGroup
	{
		readonly IItemObject m_item;

		public MoveConsumeJob(IJob parent, ActionPriority priority, IItemObject item)
			: base(parent, priority)
		{
			m_item = item;

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, item.Environment, item.Location, Positioning.Exact),
				new GetItemAssignment(this, priority, item),
				new ConsumeItemAssignment(this, priority, item),
			});
		}

		/*
		 * XXX checkvalidity tms
		protected override Progress AssignOverride(Living worker)
		{
			if (worker.Environment != m_environment)
				return Progress.Abort;

			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;

			return Progress.Ok;
		}
		*/

		public override string ToString()
		{
			return "MoveConsumeJob";
		}
	}
}
