using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class FellTreeAssignment : Assignment
	{
		readonly IntPoint3D m_location;
		readonly IEnvironment m_environment;

		public FellTreeAssignment(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, Positioning.AdjacentPlanar))
			{
				progress = JobState.Fail;
				return null;
			}

			if (CheckProgress() == JobState.Done)
			{
				progress = JobState.Done;
				return null;
			}

			var action = new FellTreeAction(v.ToDirection(), this.Priority);
			progress = JobState.Ok;
			return action;
		}

		protected override JobState ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobState.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					return JobState.Fail;

				case ActionState.Abort:
					return JobState.Abort;

				default:
					throw new Exception();
			}
		}

		JobState CheckProgress()
		{
			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return JobState.Done;
			else
				return JobState.Ok;
		}

		public override string ToString()
		{
			return "FellTree";
		}
	}
}
