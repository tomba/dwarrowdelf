using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Dwarrowdelf.AI;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObject]
	public sealed class FleeMoveAssignment : Assignment
	{

		DoubleVector3 m_fleeVector;

		public FleeMoveAssignment(IJobObserver parent)
			: base(parent)
		{
		}

		FleeMoveAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public void SetFleeVector(DoubleVector3 vector)
		{
			m_fleeVector = vector;
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			var dir = m_fleeVector.ToDirection();

			var action = DoMove(dir);

			progress = JobStatus.Ok;
			return action;
		}

		GameAction DoMove(Direction dir)
		{
			IntVector2 ov = dir.ToIntVector2();

			if (ov.IsNull)
				return new WaitAction(1);

			var env = this.Worker.Environment;

			for (int i = 0; i < 7; ++i)
			{
				var v = ov.FastRotate(((i + 1) >> 1) * (((i % 2) << 1) - 1));
				var d = env.AdjustMoveDir(this.Worker.Location, v.ToDirection());

				if (d != Direction.None)
					return new MoveAction(d);
			}

			if (this.Worker.CanMoveTo(Direction.Up))
				return new MoveAction(Direction.Up);

			if (this.Worker.CanMoveTo(Direction.Down))
				return new MoveAction(Direction.Down);

			return new WaitAction(1);
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
			return String.Format("GrazeMoveAssignment");
		}
	}
}
