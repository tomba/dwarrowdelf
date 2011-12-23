using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObjectByRef]
	public sealed class MoveMineAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		readonly MineActionType m_mineActionType;

		public MoveMineAssignment(IJobObserver parent, IEnvironmentObject environment, IntPoint3D location, MineActionType mineActionType)
			: base(parent, environment, location)
		{
			m_mineActionType = mineActionType;
		}

		MoveMineAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return GetPossiblePositioning(this.Environment, this.Location, m_mineActionType);
		}

		protected override IAssignment CreateAssignment()
		{
			return new MineAssignment(this, this.Environment, this.Location, m_mineActionType);
		}

		static DirectionSet GetPossiblePositioning(IEnvironmentObject env, IntPoint3D p, MineActionType mineActionType)
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
