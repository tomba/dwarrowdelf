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
	public class MoveConstructAssignment : AssignmentGroup
	{
		[SaveGameProperty]
		readonly IEnvironment m_environment;
		[SaveGameProperty]
		readonly IntRectZ m_area;
		[SaveGameProperty]
		readonly BuildingID m_buildingID;
		[SaveGameProperty("State")]
		int m_state;

		public MoveConstructAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntRectZ area, BuildingID buildingID)
			: base(parent, priority)
		{
			m_environment = environment;
			m_area = area;
			m_buildingID = buildingID;
		}

		protected MoveConstructAssignment(SaveGameContext ctx)
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
					assignment = new MoveAssignment(this, this.Priority, m_environment, m_area.Center, DirectionSet.Exact);
					break;

				case 1:
					assignment = new ConstructAssignment(this, this.Priority, m_environment, m_area, m_buildingID);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "MoveConstructAssignment";
		}
	}
}
