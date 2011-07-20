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
	public abstract class MoveBaseAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		public IEnvironment Environment { get; private set; }
		[SaveGameProperty]
		public IntPoint3D Location { get; private set; }
		[SaveGameProperty("State")]
		int m_state;

		public MoveBaseAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			this.Environment = environment;
			this.Location = location;
		}

		protected MoveBaseAssignment(SaveGameContext ctx)
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
					var positioning = GetPositioning();
					assignment = new MoveAssignment(this, this.Priority, this.Environment, this.Location, positioning);
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
