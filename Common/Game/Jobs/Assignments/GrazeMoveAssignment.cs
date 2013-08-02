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
	public sealed class GrazeMoveAssignment : Assignment
	{
		Group m_group;

		public GrazeMoveAssignment(IJobObserver parent, Group group)
			: base(parent)
		{
			m_group = group;
		}

		GrazeMoveAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			if (this.m_group == null)
				return PrepareNextActionOverrideHerdless(out progress);
			else
				return PrepareNextActionOverrideHerd(out progress);
		}

		GameAction PrepareNextActionOverrideHerdless(out JobStatus progress)
		{
			int i = this.Worker.World.Random.Next(8);

			var dir = DirectionExtensions.PlanarDirections[i];
			var action = DoMove(dir);

			progress = JobStatus.Ok;
			return action;
		}

		GameAction PrepareNextActionOverrideHerd(out JobStatus progress)
		{
			var center = m_group.GetCenter();

			var centerVector = center - this.Worker.Location;
			centerVector = new IntVector3(centerVector.X, centerVector.Y, 0);

			var r = this.Worker.World.Random;
			int moveStrength = m_group.GroupSize + 1;

			var l = centerVector.Length;

			if (l < moveStrength && r.Next(4) < 2)
			{
				progress = JobStatus.Ok;
				return new WaitAction(r.Next(4) + 1);
			}

			var moveVector = new IntVector3(r.Next(-moveStrength, moveStrength + 1), r.Next(-moveStrength, moveStrength + 1), 0);

			var v = centerVector + moveVector;

			var dir = v.ToDirection();

			if (dir == Direction.None)
			{
				progress = JobStatus.Ok;
				return new WaitAction(r.Next(4) + 1);
			}

			var action = DoMove(dir);

			progress = JobStatus.Ok;
			return action;
		}

		private MoveAction DoMove(Direction dir)
		{
			var env = this.Worker.Environment;

			dir = env.AdjustMoveDir(this.Worker.Location, dir);

			return new MoveAction(dir);
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
