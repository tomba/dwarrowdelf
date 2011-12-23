using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public sealed class DestructBuildingAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IBuildingObject m_building;

		public DestructBuildingAssignment(IJobObserver parent, IBuildingObject building)
			: base(parent)
		{
			m_building = building;
		}

		DestructBuildingAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new DestructBuildingAction(m_building.ObjectID);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return String.Format("DestructBuildingAssignment");
		}
	}
}
