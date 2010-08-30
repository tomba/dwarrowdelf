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

		public void ActionRequired(ActionPriority priority)
		{
			if (priority == ActionPriority.Idle)
			{
				if (this.Worker.HasAction && this.Worker.CurrentAction.Priority >= priority)
					return;

				if (this.Worker.HasAction)
					this.Worker.CancelAction();
				var a = GetNewAction();
				a.Priority = priority;
				this.Worker.DoAction(a);
			}
		}

		public void ActionProgress(ActionProgressEvent e)
		{
		}

		GameAction GetNewAction()
		{
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(m_random.Next(3) + 1);
			else
			{
				IntVector v = new IntVector(1, 1);
				v = v.Rotate(45 * m_random.Next(8));
				Direction dir = v.ToDirection();

				if (dir == Direction.None)
					throw new Exception();

				action = new MoveAction(dir);
			}

			return action;
		}
	}
}
