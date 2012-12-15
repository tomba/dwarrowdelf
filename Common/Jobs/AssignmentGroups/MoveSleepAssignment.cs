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
	[SaveGameObjectByRef]
	public sealed class MoveSleepAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		public IItemObject Bed { get; private set; }
		[SaveGameProperty("State")]
		int m_state;

		public MoveSleepAssignment(IJobObserver parent, IItemObject bed)
			: base(parent)
		{
			this.Bed = bed;
			m_state = 0;
		}

		MoveSleepAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStatusChanged(JobStatus status)
		{
			Debug.Assert(status != JobStatus.Ok);

			base.OnStatusChanged(status);
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == 1)
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
					assignment = new MoveAssignment(this, this.Bed.Environment, this.Bed.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new SleepAssignment(this, this.Bed);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "MoveSleepAssignment";
		}
	}
}
