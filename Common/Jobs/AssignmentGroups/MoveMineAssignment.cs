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
	public class MoveMineAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		readonly IEnvironment m_environment;
		[SaveGameProperty]
		readonly IntPoint3D m_location;
		[SaveGameProperty]
		readonly MineActionType m_mineActionType;
		[SaveGameProperty("State")]
		int m_state;

		public MoveMineAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;
			m_mineActionType = mineActionType;
		}


		protected MoveMineAssignment(SaveGameContext ctx)
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
					var positioning = GetPossiblePositioning(m_environment, m_location, m_mineActionType);
					assignment = new MoveAssignment(this, this.Priority, m_environment, m_location, positioning);
					break;

				case 1:
					assignment = new MineAssignment(this, this.Priority, m_environment, m_location, m_mineActionType);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		static DirectionSet GetPossiblePositioning(IEnvironment env, IntPoint3D p, MineActionType mineActionType)
		{
			DirectionSet pos;

			var down = p + Direction.Down;

			switch (mineActionType)
			{
				case MineActionType.Mine:
					pos = DirectionSet.Planar;

					if (EnvironmentHelpers.CanMoveFrom(env, down, Direction.Up))
						pos |= DirectionSet.Down;

					break;

				case MineActionType.Stairs:
					pos = DirectionSet.Planar | DirectionSet.Up;

					if (EnvironmentHelpers.CanMoveFrom(env, down, Direction.Up))
						pos |= DirectionSet.Down;

					break;

				default:
					throw new Exception();
			}

			return pos;
		}

		public override string ToString()
		{
			return "MoveMineAssignment";
		}
	}
}
