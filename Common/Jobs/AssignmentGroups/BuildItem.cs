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
	public class BuildItem : AssignmentGroup
	{
		[SaveGameProperty]
		IBuildingObject m_workplace;
		[SaveGameProperty]
		IItemObject[] m_items;
		[SaveGameProperty]
		ItemID m_dstItemID;

		[SaveGameProperty("State")]
		int m_state;

		public BuildItem(IJob parent, ActionPriority priority, IBuildingObject workplace, IItemObject[] items, ItemID dstItemID)
			: base(parent, priority)
		{
			m_workplace = workplace;
			m_items = items;
			m_dstItemID = dstItemID;
		}

		protected BuildItem(SaveGameContext ctx)
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
			if (m_state == 1)
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
					assignment = new MoveAssignment(this, this.Priority, m_workplace.Environment, m_workplace.Area.Center, DirectionSet.Exact);
					break;

				case 1:
					assignment = new BuildItemAssignment(this, this.Priority, m_items, m_dstItemID);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
