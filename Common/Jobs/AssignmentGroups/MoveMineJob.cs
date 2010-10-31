using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class MoveMineJob : StaticAssignmentGroup
	{
		readonly IEnvironment m_environment;
		readonly IntPoint3D m_location;

		public MoveMineJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: this(parent, priority, environment, location, mineActionType, Positioning.AdjacentPlanarUpDown)
		{
		}

		public MoveMineJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType, Positioning positioning)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, m_environment, m_location, positioning),
				new MineAssignment(this, priority, m_environment, m_location, mineActionType),
			});
		}

		public MoveMineJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType, IntPoint3D miningLocation)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, m_environment, miningLocation, Positioning.Exact),
				new MineAssignment(this, priority, m_environment, m_location, mineActionType),
			});
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
			return "MoveMineJob";
		}
	}
}
