using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class MonsterActor : IActor
	{
		ServerGameObject m_object;
		GameAction m_currentAction;
		Random m_random = new Random();

		public MonsterActor(ServerGameObject ob)
		{
			m_object = ob;
			m_currentAction = GetNewAction();
		}

		#region IActor Members

		public void EnqueueAction(GameAction action)
		{
			throw new NotImplementedException();
		}

		public GameAction DequeueAction()
		{
			if (m_currentAction == null)
				m_currentAction = GetNewAction();

			GameAction action = m_currentAction;
			m_currentAction = null;
			return action;
		}

		GameAction GetNewAction()
		{
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(m_object, m_random.Next(3) + 1);
			else
				action = new MoveAction(m_object, (Direction)m_random.Next(8));

			return action;
		}

		public GameAction PeekAction()
		{
			if (m_currentAction == null)
				m_currentAction = GetNewAction();

			return m_currentAction;
		}

		public event Action ActionQueuedEvent;

		#endregion
	}
}
