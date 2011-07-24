﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject(UseRef = true)]
	public class ConstructAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IEnvironment m_environment;
		[SaveGameProperty]
		readonly IntRectZ m_area;
		[SaveGameProperty]
		readonly BuildingID m_buildingID;

		public ConstructAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntRectZ area, BuildingID buildingID)
			: base(parent, priority)
		{
			m_environment = environment;
			m_area = area;
			m_buildingID = buildingID;
		}

		protected ConstructAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new ConstructAction(m_environment, m_area, m_buildingID, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return String.Format("ConstructAssignment");
		}

	}
}
