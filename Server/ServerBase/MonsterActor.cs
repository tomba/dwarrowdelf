
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	public class MonsterActor : Jobs.JobAI
	{
		Random m_random;

		public MonsterActor(Living living) : base(living)
		{
			m_random = new Random(GetHashCode());
		}
		/*
		public override void ActionRequired(ActionPriority priority)
		{
			if (priority != ActionPriority.High || this.Worker.HasAction)
				return;

			var a = GetNewRandomMoveAction();
			a.Priority = priority;
			this.Worker.DoAction(a);
		}

		public override void ActionProgress(ActionProgressEvent e)
		{
		}
		*/
		protected override Jobs.IActionJob GetJob(ILiving worker, ActionPriority priority)
		{
			var env = worker.Environment;
			var l = worker.Location;

			for (int i = 0; i < 20; i++)
			{
				l += Direction.North;

				if (env.GetInteriorID(l) == InteriorID.Wall)
				{
					var job = new Jobs.MoveMineJob(null, priority, env, l);
					return job;
				}
			}

			return null;
		}

		GameAction GetNewRandomMoveAction(ActionPriority priority)
		{
			GameAction action;
			/*
			if (m_random.Next(4) == 0)
				action = new WaitAction(m_random.Next(3) + 1);
			else*/
			{
				IntVector v = new IntVector(1, 1);
				v = v.Rotate(45 * m_random.Next(8));
				Direction dir = v.ToDirection();

				if (dir == Direction.None)
					throw new Exception();

				action = new MoveAction(dir, priority);
			}

			return action;
		}
	}
}
