using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	/// <summary>
	/// For debug only
	/// </summary>
	[SaveGameObjectByRef]
	public class GenericDebugAssignment : Assignment
	{
		[SaveGameProperty("Action")]
		readonly GameAction m_action;

		public GenericDebugAssignment(IJobObserver parent, GameAction action)
			: base(parent)
		{
			m_action = action;
		}

		protected GenericDebugAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			progress = JobStatus.Ok;
			return m_action;
		}

		public override string ToString()
		{
			return "GenericAssignment";
		}
	}
}
