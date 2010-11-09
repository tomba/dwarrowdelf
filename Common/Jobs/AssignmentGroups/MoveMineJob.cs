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
		readonly MineActionType m_mineActionType;
		MoveAssignment m_moveAssignment;

		public MoveMineJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;
			m_mineActionType = mineActionType;

			var positioning = GetPossiblePositioning(environment, location, mineActionType);

			m_moveAssignment = new MoveAssignment(this, priority, m_environment, m_location, positioning);

			SetAssignments(new IAssignment[] {
				m_moveAssignment,
				new MineAssignment(this, priority, m_environment, m_location, mineActionType),
			});
		}

		protected override JobState AssignOverride(ILiving worker)
		{
			var positioning = GetPossiblePositioning(m_environment, m_location, m_mineActionType);

			m_moveAssignment.Positioning = positioning;

			return JobState.Ok;
		}

		static Positioning GetPossiblePositioning(IEnvironment env, IntPoint3D p, MineActionType mineActionType)
		{
			Positioning pos;

			var down = p + Direction.Down;

			switch (mineActionType)
			{
				case MineActionType.Mine:
					if (EnvironmentHelpers.CanMoveFrom(env, down, Direction.Up))
						pos = Positioning.AdjacentPlanarDown;
					else
						pos = Positioning.AdjacentPlanar;

					break;

				case MineActionType.Stairs:
					if (EnvironmentHelpers.CanMoveFrom(env, down, Direction.Up))
						pos = Positioning.AdjacentPlanarUpDown;
					else
						pos = Positioning.AdjacentPlanarUp;

					break;

				default:
					throw new Exception();
			}

			return pos;
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
