using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class InteractiveActor : IActor
	{
		Queue<GameAction> m_actionQueue;

		public InteractiveActor()
		{
			m_actionQueue = new Queue<GameAction>();
		}

		public void EnqueueAction(GameAction action)
		{
			lock (m_actionQueue)
			{
				m_actionQueue.Enqueue(action);
			}

			if (ActionQueuedEvent != null)
				ActionQueuedEvent();
		}

		#region IActor Members

		public event Action ActionQueuedEvent;

		public void RemoveAction(GameAction action)
		{
			lock (m_actionQueue)
			{
				GameAction topAction = m_actionQueue.Peek();

				if (topAction == action)
					m_actionQueue.Dequeue();
			}
		}

		public GameAction GetCurrentAction()
		{
			lock (m_actionQueue)
			{
				if (m_actionQueue.Count == 0)
					return null;

				return m_actionQueue.Peek();
			}
		}

		public bool HasAction
		{
			get 
			{
				lock (m_actionQueue)
					return m_actionQueue.Count > 0;
			}
		}

		#endregion
	}
}
