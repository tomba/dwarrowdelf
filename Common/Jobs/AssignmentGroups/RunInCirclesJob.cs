using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class RunInCirclesJob : StaticAssignmentGroup
	{
		readonly IEnvironment m_environment;

		public RunInCirclesJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;

			var jobs = new IAssignment[] {
				new MoveAssignment(this, priority, environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact),
				new MoveAssignment(this, priority, environment, new IntPoint3D(14, 18, 9), DirectionSet.Exact),
				new MoveAssignment(this, priority, environment, new IntPoint3D(14, 28, 9), DirectionSet.Exact),
				new MoveAssignment(this, priority, environment, new IntPoint3D(2, 28, 9), DirectionSet.Exact),
				new MoveAssignment(this, priority, environment, new IntPoint3D(2, 18, 9), DirectionSet.Exact),
			};

			SetAssignments(jobs);
		}

		public override string ToString()
		{
			return "RunInCirclesJob";
		}
	}
}
