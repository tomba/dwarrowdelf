using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject(UseRef = true)]
	public class MoveConsumeJob : AssignmentGroup
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;
		[SaveGameProperty("State")]
		int m_state;

		public MoveConsumeJob(IJob parent, ActionPriority priority, IItemObject item)
			: base(parent, priority)
		{
			m_item = item;
		}

		protected MoveConsumeJob(SaveGameContext ctx)
			: base(ctx)
		{
		}


		protected override JobStatus AssignOverride(ILiving worker)
		{
			m_state = 0;
			return JobStatus.Ok;
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == 2)
				SetStatus(Jobs.JobStatus.Done);
			else
				m_state = m_state + 1;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, this.Priority, m_item.Environment, m_item.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new GetItemAssignment(this, this.Priority, m_item);
					break;

				case 2:
					assignment = new ConsumeItemAssignment(this, this.Priority, m_item);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}
		public override string ToString()
		{
			return "MoveConsumeJob";
		}
	}
}
