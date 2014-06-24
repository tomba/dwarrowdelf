using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class FellTreeAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IntVector3 m_location;
		[SaveGameProperty]
		readonly IEnvironmentObject m_environment;

		public FellTreeAssignment(IJobObserver parent, IEnvironmentObject environment, IntVector3 location)
			: base(parent)
		{
			m_environment = environment;
			m_location = location;
		}

		FellTreeAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, DirectionSet.Planar))
			{
				progress = JobStatus.Fail;
				return null;
			}

			var action = new FellTreeAction(v.ToDirection());
			progress = JobStatus.Ok;
			return action;
		}

		public override string ToString()
		{
			return "FellTree";
		}
	}
}
