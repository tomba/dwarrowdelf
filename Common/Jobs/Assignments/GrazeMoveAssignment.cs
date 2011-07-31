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
	[SaveGameObject(UseRef = true)]
	public class GrazeMoveAssignment : Assignment
	{
		HerbivoreHerd m_herd;

		public GrazeMoveAssignment(IJob parent, ActionPriority priority, HerbivoreHerd herd)
			: base(parent, priority)
		{
			m_herd = herd;
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

		protected override JobStatus AssignOverride(ILiving worker)
		{
			return Jobs.JobStatus.Ok;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			if (this.m_herd == null)
				return PrepareNextActionOverrideHerdless(out progress);
			else
				return PrepareNextActionOverrideHerd(out progress);
		}

		GameAction PrepareNextActionOverrideHerdless(out JobStatus progress)
		{
			int i = this.Worker.World.Random.Next(8);

			var dir = DirectionExtensions.PlanarDirections[i];
			var action = DoMove(dir);

			progress = Jobs.JobStatus.Ok;
			return action;
		}

		GameAction PrepareNextActionOverrideHerd(out JobStatus progress)
		{
			var center = m_herd.GetCenter();

			var centerVector = center - this.Worker.Location;

			var r = this.Worker.World.Random;
			int moveStrength = m_herd.HerdSize + 1;

			var l = centerVector.Length;

			if (l < moveStrength && r.Next(4) < 2)
			{
				progress = Jobs.JobStatus.Ok;
				return new WaitAction(r.Next(4) + 1, this.Priority);
			}

			var moveVector = new IntVector3D(r.Next(-moveStrength, moveStrength + 1), r.Next(-moveStrength, moveStrength + 1), center.Z);

			var v = centerVector + moveVector;

			var dir = v.ToDirection();

			Trace.TraceInformation("{0} -> {1}: {2}", center, this.Worker.Location, dir);

			var action = DoMove(dir);

			progress = Jobs.JobStatus.Ok;
			return action;
		}

		private MoveAction DoMove(Direction dir)
		{
			var env = this.Worker.Environment;

			var flr = env.GetTerrainID(this.Worker.Location);

			if (flr.IsSlope() && flr.ToDir() == dir)
			{
				dir |= Direction.Up;
			}
			else
			{
				var p = this.Worker.Location + dir + Direction.Down;
				if (env.Contains(p))
				{
					flr = env.GetTerrainID(this.Worker.Location + dir + Direction.Down);
					if (flr.IsSlope() && flr.ToDir().Reverse() == dir)
						dir |= Direction.Down;
				}
			}

			return new MoveAction(dir, this.Priority);
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