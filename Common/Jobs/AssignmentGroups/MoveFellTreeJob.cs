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
	public class MoveFellTreeJob : StaticAssignmentGroup
	{
		[SaveGameProperty]
		readonly IEnvironment m_environment;
		[SaveGameProperty]
		readonly IntPoint3D m_location;

		public MoveFellTreeJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, m_environment, m_location, DirectionSet.Planar),
				new FellTreeAssignment(this, priority, m_environment, m_location),
			});
		}

		protected MoveFellTreeJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		/*
		 * XXX checkvalidity tms
		protected override Progress AssignOverride(Living worker)
		{
			if (worker.Environment != m_environment)
				return Progress.Abort;

			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;

			return Progress.Ok;
		}
		*/

		public override string ToString()
		{
			return "MoveFellTreeJob";
		}
	}
}
