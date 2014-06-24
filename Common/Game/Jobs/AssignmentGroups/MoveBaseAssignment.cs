using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObject]
	public abstract class MoveBaseAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		public IEnvironmentObject Environment { get; private set; }
		[SaveGameProperty]
		public IntVector3 Location { get; private set; }
		[SaveGameProperty("State")]
		int m_state;

		protected MoveBaseAssignment(IJobObserver parent, IEnvironmentObject environment, IntVector3 location)
			: base(parent)
		{
			this.Environment = environment;
			this.Location = location;
			m_state = 0;
		}

		protected MoveBaseAssignment(SaveGameContext ctx)
			: base(ctx)
		{
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
					var positioning = GetPositioning();
					assignment = new MoveAssignment(this, this.Environment, this.Location, positioning);
					break;

				case 1:
					assignment = CreateAssignment();
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		protected abstract IAssignment CreateAssignment();
		protected abstract DirectionSet GetPositioning();
	}
}
