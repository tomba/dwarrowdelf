using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class ConstructAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IEnvironmentObject m_environment;
		[SaveGameProperty]
		readonly ConstructMode m_mode;
		[SaveGameProperty]
		readonly IntPoint3 m_location;
		[SaveGameProperty]
		readonly IItemObject[] m_items;

		public ConstructAssignment(IJobObserver parent, ConstructMode mode, IEnvironmentObject environment, IntPoint3 location, IItemObject[] items)
			: base(parent)
		{
			m_mode = mode;
			m_environment = environment;
			m_location = location;
			m_items = items;
		}

		ConstructAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var action = new ConstructAction(m_mode, m_location, m_items);
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return String.Format("ConstructAssignment");
		}
	}
}
