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
	public sealed class RandomMoveAssignment : Assignment
	{
		[SaveGameProperty("Dir")]
		Direction m_dir;

		public RandomMoveAssignment(IJobObserver parent)
			: base(parent)
		{
		}

		RandomMoveAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
		}

		protected override void AssignOverride(ILivingObject worker)
		{
			int i = worker.World.Random.Next(8);
			m_dir = DirectionExtensions.PlanarDirections[i];
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var random = this.Worker.World.Random;

			int rand = random.Next(100);

			GameAction action = null;

			Direction dir = m_dir;

			if (rand < 25)
			{
				dir = Direction.None;
			}
			else if (rand < 50)
			{
			}
			else if (rand < 75)
			{
				var v = dir.ToIntVector3();
				v = v.FastRotate(random.Next() % 2 == 0 ? 1 : -1);
				dir = v.ToDirection();
			}
			else
			{
				var v = dir.ToIntVector3();
				v = v.FastRotate(random.Next() % 2 == 0 ? 2 : -2);
				dir = v.ToDirection();
			}

			if (dir == Direction.None)
			{
				action = new WaitAction(random.Next(4) + 1);
			}
			else
			{
				var env = this.Worker.Environment;
				m_dir = dir;

				IntVector2 ov = dir.ToIntVector2();

				for (int i = 0; i < 7; ++i)
				{
					var v = ov.FastRotate(((i + 1) >> 1) * (((i % 2) << 1) - 1));
					var d = env.AdjustMoveDir(this.Worker.Location, v.ToDirection());

					if (d != Direction.None)
						action = new MoveAction(d);
				}

				if (action == null && this.Worker.CanMoveTo(Direction.Up))
					action = new MoveAction(Direction.Up);

				if (action == null && this.Worker.CanMoveTo(Direction.Down))
					action = new MoveAction(Direction.Down);

				if (action == null)
					action = new WaitAction(random.Next(4) + 1);
			}

			progress = JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionDoneOverride(ActionState actionStatus)
		{
			switch (actionStatus)
			{
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
