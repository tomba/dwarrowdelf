using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject(UseRef = true)]
	public class MoveConsumeAssignment : AssignmentGroup
	{
		[SaveGameProperty("Item")]
		readonly IItemObject m_item;
		[SaveGameProperty("State")]
		int m_state;

		public MoveConsumeAssignment(IJob parent, IItemObject item)
			: base(parent)
		{
			m_item = item;
			m_item.ReservedBy = this;
		}

		protected MoveConsumeAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStatusChanged(JobStatus status)
		{
			Debug.Assert(status != Jobs.JobStatus.Ok);

			m_item.ReservedBy = null;

			base.OnStatusChanged(status);
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
					assignment = new MoveAssignment(this, m_item.Environment, m_item.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new GetItemAssignment(this, m_item);
					break;

				case 2:
					assignment = new ConsumeItemAssignment(this, m_item);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "MoveConsumeAssignment";
		}
	}
}
