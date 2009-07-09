//#define STAYSTILL

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

		GameAction GetNewAction()
		{
#if STAYSTILL
			return null;
#else
			GameAction action;

			if (m_random.Next(4) == 0)
				action = new WaitAction(0, m_object, m_random.Next(3) + 1);
			else
				action = new MoveAction(0, m_object, (Direction)m_random.Next(8));

			return action;
#endif
		}

		#region IActor Members

		public void RemoveAction(GameAction action)
		{
			m_currentAction = null;
		}

		public GameAction GetCurrentAction()
		{
			if (m_currentAction == null)
				m_currentAction = GetNewAction();

			return m_currentAction;
		}

		public bool HasAction
		{
			get { return true; }
		}

		public event Action ActionQueuedEvent;

		#endregion
	}
}
