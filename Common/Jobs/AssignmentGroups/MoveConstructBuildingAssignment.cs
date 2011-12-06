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
	public class MoveConstructBuildingAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		readonly IntRectZ m_area;
		[SaveGameProperty]
		readonly BuildingID m_buildingID;

		public MoveConstructBuildingAssignment(IJobObserver parent, IEnvironmentObject environment, IntRectZ area, BuildingID buildingID)
			: base(parent, environment, area.Center)
		{
			m_area = area;
			m_buildingID = buildingID;
		}

		protected MoveConstructBuildingAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return DirectionSet.Exact;
		}

		protected override IAssignment CreateAssignment()
		{
			return new ConstructBuildingAssignment(this, this.Environment, m_area, m_buildingID);
		}

		public override string ToString()
		{
			return "MoveConstructBuildingAssignment";
		}
	}
}
