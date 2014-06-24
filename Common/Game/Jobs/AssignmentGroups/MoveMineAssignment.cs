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
	public sealed class MoveMineAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		readonly MineActionType m_mineActionType;

		public MoveMineAssignment(IJobObserver parent, IEnvironmentObject environment, IntVector3 location, MineActionType mineActionType)
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
			return this.Environment.GetPossibleMiningPositioning(this.Location, m_mineActionType);
		}

		protected override IAssignment CreateAssignment()
		{
			return new MineAssignment(this, this.Environment, this.Location, m_mineActionType);
		}

		public override string ToString()
		{
			return "MoveMineAssignment";
		}
	}
}
