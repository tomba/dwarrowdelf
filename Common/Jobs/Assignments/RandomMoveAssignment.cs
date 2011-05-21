using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	[GameObject(UseRef = true)]
	public class RandomMoveAssignment : Assignment
	{
		[GameProperty("Environment")]
		readonly IEnvironment m_environment;
		[GameProperty("Dir")]
		Direction m_dir;

		public RandomMoveAssignment(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			int i = worker.World.Random.Next(8);
			m_dir = DirectionExtensions.PlanarDirections[i];
			return Jobs.JobStatus.Ok;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var random = this.Worker.World.Random;

			int i = random.Next(100);

			GameAction action;

			Direction dir = m_dir;

			if (i < 25)
			{
				dir = Direction.None;
			}
			else if (i < 50)
			{
			}
			else if (i < 75)
			{
				var v = new IntVector3D(dir);
				v = v.FastRotate(random.Next() % 2 == 0 ? 1 : -1);
				dir = v.ToDirection();
			}
			else
			{
				var v = new IntVector3D(dir);
				v = v.FastRotate(random.Next() % 2 == 0 ? 2 : -2);
				dir = v.ToDirection();
			}

			if (dir == Direction.None)
			{
				action = new WaitAction(random.Next(4) + 1, this.Priority);
			}
			else
			{
				m_dir = dir;

				var flr = m_environment.GetFloorID(this.Worker.Location);

				if (flr.IsSlope() && flr.ToDir() == dir)
					dir |= Direction.Up;
				else
				{
					var p = this.Worker.Location + dir + Direction.Down;
					if (m_environment.Bounds.Contains(p))
					{
						flr = m_environment.GetFloorID(this.Worker.Location + dir + Direction.Down);
						if (flr.IsSlope() && flr.ToDir().Reverse() == dir)
							dir |= Direction.Down;
					}
				}

				action = new MoveAction(dir, this.Priority);
			}

			progress = Jobs.JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
				case ActionState.Done:
				case ActionState.Fail:
				case ActionState.Abort:
					return JobStatus.Ok;

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			return String.Format("RandomMove");
		}

	}
}
