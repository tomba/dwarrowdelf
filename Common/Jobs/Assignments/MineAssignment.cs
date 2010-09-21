using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.Assignments
{
	public class MineAssignment : Assignment
	{
		readonly IntPoint3D m_location;
		readonly IEnvironment m_environment;

		public MineAssignment(IJob job, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(job, priority)
		{
			m_environment = environment;
			m_location = location;
		}

		protected override GameAction PrepareNextActionOverride(out Progress progress)
		{
			var v = m_location - this.Worker.Location;

			if (!v.IsAdjacent2D)
			{
				progress = Progress.Fail;
				return null;
			}

			if (CheckProgress() == Progress.Done)
			{
				progress = Progress.Done;
				return null;
			}

			var action = new MineAction(v.ToDirection(), this.Priority);
			progress = Progress.Ok;
			return action;
		}

		protected override Progress ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return Progress.Ok;

				case ActionState.Done:
					return CheckProgress();

				case ActionState.Fail:
					return Progress.Fail;

				case ActionState.Abort:
					return Progress.Abort;

				default:
					throw new Exception();
			}
		}

		Progress CheckProgress()
		{
			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return "Mine";
		}
	}
}
