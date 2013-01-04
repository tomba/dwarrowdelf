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
	[SaveGameObject]
	public sealed class MoveConsumeAssignment : AssignmentGroup
	{
		[SaveGameProperty("Item")]
		public IItemObject Item { get; private set; }
		[SaveGameProperty("State")]
		int m_state;

		public MoveConsumeAssignment(IJobObserver parent, IItemObject item)
			: base(parent)
		{
			this.Item = item;
			m_state = 0;
		}

		MoveConsumeAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == 2)
				SetStatus(JobStatus.Done);
			else
				m_state = m_state + 1;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new GetItemAssignment(this, this.Item);
					break;

				case 2:
					assignment = new ConsumeItemAssignment(this, this.Item);
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
