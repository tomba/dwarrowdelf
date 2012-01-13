﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Dwarrowdelf.AI;

namespace Dwarrowdelf.Jobs.Assignments
{
	[SaveGameObjectByRef]
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
			IntVector ov = new IntVector(dir);

			var env = this.Worker.Environment;

			for (int i = 0; i < 7; ++i)
			{
				var v = ov.FastRotate(((i + 1) >> 1) * (((i % 2) << 1) - 1));
				var d = TryPlanarDir(v.ToDirection());

				if (d.HasValue)
					return new MoveAction(d.Value);
			}

			if (EnvironmentHelpers.CanMoveFromTo(this.Worker, Direction.Up))
				return new MoveAction(Direction.Up);

			if (EnvironmentHelpers.CanMoveFromTo(this.Worker, Direction.Down))
				return new MoveAction(Direction.Down);

			return new WaitAction(1);
		}

		Direction? TryPlanarDir(Direction d)
		{
			var env = this.Worker.Environment;

			var srcTerrainID = env.GetTerrainID(this.Worker.Location);

			if (srcTerrainID.IsSlope() && srcTerrainID.ToDir() == d)
			{
				d |= Direction.Up;
				if (EnvironmentHelpers.CanMoveFromTo(this.Worker, d))
					return d;
				else
					return null;
			}

			var p = this.Worker.Location + d + Direction.Down;

			if (env.Contains(p))
			{
				var dstTerrainID = env.GetTerrainID(p);

				if (dstTerrainID.IsSlope() && dstTerrainID.ToDir().Reverse() == d)
				{
					d |= Direction.Down;
					if (EnvironmentHelpers.CanMoveFromTo(this.Worker, d))
						return d;
					else
						return null;
				}
			}

			if (EnvironmentHelpers.CanMoveFromTo(this.Worker, d))
				return d;
			else
				return null;
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