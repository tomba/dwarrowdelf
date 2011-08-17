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
	public class MoveDestructBuildingAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		readonly IBuildingObject m_building;

		public MoveDestructBuildingAssignment(IJob parent, IBuildingObject building)
			: base(parent, building.Environment, building.Area.Center)
		{
			m_building = building;
		}

		protected MoveDestructBuildingAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return DirectionSet.Exact;
		}

		protected override IAssignment CreateAssignment()
		{
			return new DestructBuildingAssignment(this, m_building);
		}

		public override string ToString()
		{
			return "MoveDestructBuildingAssignment";
		}
	}
}
