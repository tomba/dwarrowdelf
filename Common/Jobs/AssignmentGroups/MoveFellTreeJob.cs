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
	public class MoveFellTreeJob : AssignmentGroup
	{
		[SaveGameProperty]
		readonly IEnvironment m_environment;
		[SaveGameProperty]
		readonly IntPoint3D m_location;
		[SaveGameProperty("State")]
		int m_state;

		public MoveFellTreeJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;
		}

		protected MoveFellTreeJob(SaveGameContext ctx)
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
					assignment = new MoveAssignment(this, this.Priority, m_environment, m_location, DirectionSet.Planar);
					break;

				case 1:
					assignment = new FellTreeAssignment(this, this.Priority, m_environment, m_location);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}


		public override string ToString()
		{
			return "MoveFellTreeJob";
		}
	}
}
