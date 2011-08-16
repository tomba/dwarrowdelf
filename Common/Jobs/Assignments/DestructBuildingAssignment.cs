using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class DestructBuildingAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IBuildingObject m_building;

		public DestructBuildingAssignment(IJob parent, ActionPriority priority, IBuildingObject building)
			: base(parent, priority)
		{
			m_building = building;
		}

		protected DestructBuildingAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new DestructBuildingAction(m_building.ObjectID, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return String.Format("DestructBuildingAssignment");
		}
	}
}
