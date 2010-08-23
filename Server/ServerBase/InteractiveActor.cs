using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	class InteractiveActor : IActor
	{
		Living m_object;
		Random m_random;

		public InteractiveActor(Living ob)
		{
			m_random = new Random(GetHashCode());
			m_object = ob;
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

		#region IActor Members

		public void DetermineIdleAction()
		{
			if (m_object.HasAction)
				return;
			var a = GetNewAction();
			m_object.EnqueueAction(a);
		}

		public void DeterminePriorityAction()
		{
		}

		#endregion
	}
}
