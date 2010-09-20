using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	class InteractiveActor : Jobs.JobAI
	{
		//Living Worker { get; set; }
		Random m_random;

		public InteractiveActor(Living ob) : base(ob)
		{
			m_random = new Random(GetHashCode());
			//this.Worker = ob;
		}

		protected override bool CheckForAbortOtherAction(ActionPriority priority)
		{
			/*
			if (priority < ActionPriority.High)
				return false;

			if (this.Worker.World.TickNumber % 20 == 0)
				return true;
			*/
			return false;
		}

		protected override bool CheckForAbortOurJob(ActionPriority priority)
		{
			/*
			if (priority < ActionPriority.High)
				return false;

			if (this.Worker.World.TickNumber % 20 == 0)
				return true;
			*/
			return false;
		}

		protected override Jobs.IActionJob GetJob(ILiving worker, ActionPriority priority)
		{
			/*
			if (priority == ActionPriority.High)
			{
				if (this.Worker.World.TickNumber % 20 == 0)
					return new Jobs.WaitActionJob(null, priority, 8);
				else
					return null;
			}
			 */
			/*
			return new Jobs.WaitActionJob(null, priority, 4);
			*/

			if (priority == ActionPriority.High)
				return null;

			return new Jobs.SerialActionJobs.LoiterJob(null, priority, worker.Environment);
		}

#if asd
		public GameAction ActionRequired(ActionPriority priority)
		{
			if (priority == ActionPriority.Idle)
			{
				if (this.Worker.HasAction && this.Worker.CurrentAction.Priority >= priority)
					return null;

				if (this.Worker.HasAction)
					this.Worker.CancelAction();
				var a = GetNewAction(priority);

				return a;
			}
			else
				return null;
		}

		public void ActionProgress(ActionProgressChange e)
		{
		}

		GameAction GetNewAction(ActionPriority priority)
		{
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(m_random.Next(3) + 1, priority);
			else
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
#endif

	}
}
