using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class LoiterJob : AssignmentGroup
	{
		readonly IEnvironment m_environment;
		int m_state;

		public LoiterJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;
		}

		protected override JobState AssignOverride(ILiving worker)
		{
			m_state = 0;
			return JobState.Ok;
		}

		protected override IAssignment GetNextAssignment()
		{
			int state = m_state++;

			switch (state)
			{
				case 0:
					return new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact);

				case 1:
					return new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(14, 18, 9), DirectionSet.Exact);

				case 2:
					return new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(14, 28, 9), DirectionSet.Exact);

				case 3:
					return new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 28, 9), DirectionSet.Exact);

				case 4:
					return new MoveAssignment(this, this.Priority, m_environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact);

				case 5:
					m_state = 0;
					return GetNextAssignment();

				default:
					throw new Exception();
			}
		}

		protected override JobState CheckProgress()
		{
			return Jobs.JobState.Ok;
		}

		public override string ToString()
		{
			return "LoiterJob";
		}
	}
}
