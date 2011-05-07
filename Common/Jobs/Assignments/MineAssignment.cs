using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class MineAssignment : Assignment
	{
		[GameProperty]
		readonly IntPoint3D m_location;
		[GameProperty]
		readonly MineActionType m_mineActionType;
		[GameProperty]
		readonly IEnvironment m_environment;

		public MineAssignment(IJob job, ActionPriority priority, IEnvironment environment, IntPoint3D location, MineActionType mineActionType)
			: base(job, priority)
		{
			m_environment = environment;
			m_location = location;
			m_mineActionType = mineActionType;
		}

		protected override GameAction PrepareNextActionOverride(out JobState progress)
		{
			var v = m_location - this.Worker.Location;

			if (!this.Worker.Location.IsAdjacentTo(m_location, DirectionSet.PlanarUpDown))
			{
				progress = JobState.Fail;
				return null;
			}

			if (CheckProgress() == JobState.Done)
			{
				progress = JobState.Done;
				return null;
			}

			var action = new MineAction(v.ToDirection(), m_mineActionType, this.Priority);
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
			var inter = m_environment.GetInterior(m_location);

			if (inter.ID == InteriorID.Undefined || inter.IsMineable)
				return JobState.Ok;
			else
				return JobState.Done;
		}

		public override string ToString()
		{
			return "Mine";
		}
	}
}
