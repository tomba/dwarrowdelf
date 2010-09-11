using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	class InteractiveActor : Jobs.IAI
	{
		Living Worker { get; set; }
		Random m_random;

		public InteractiveActor(Living ob)
		{
			m_random = new Random(GetHashCode());
			this.Worker = ob;
		}

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

			if (true || m_random.Next(4) == 0)
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
	}
}
