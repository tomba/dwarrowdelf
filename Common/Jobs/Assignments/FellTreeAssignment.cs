using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
	public class FellTreeAssignment : Assignment
	{
		[SaveGameProperty]
		readonly IntPoint3D m_location;
		[SaveGameProperty]
		readonly IEnvironment m_environment;

		public FellTreeAssignment(IJobObserver parent, IEnvironment environment, IntPoint3D location)
			: base(parent)
		{
			m_environment = environment;
			m_location = location;
		}

		protected FellTreeAssignment(SaveGameContext ctx)
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

			if (CheckProgress() == JobStatus.Done)
			{
				progress = JobStatus.Done;
				return null;
			}

			var action = new FellTreeAction(v.ToDirection());
			progress = JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionDoneOverride(ActionState actionStatus)
		{
			switch (actionStatus)
			{
				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					return JobStatus.Fail;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		JobStatus CheckProgress()
		{
			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return JobStatus.Done;
			else
				return JobStatus.Ok;
		}

		public override string ToString()
		{
			return "FellTree";
		}
	}
}
