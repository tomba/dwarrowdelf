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

		#region IActor Members

		public event Action ActionQueuedEvent;

		public void EnqueueAction(GameAction action)
		{
			lock (m_actionQueue)
			{
				m_actionQueue.Enqueue(action);
			}

			if (ActionQueuedEvent != null)
				ActionQueuedEvent();
		}

		public GameAction DequeueAction()
		{
			lock (m_actionQueue)
			{
				return m_actionQueue.Dequeue();
			}
		}

		public GameAction PeekAction()
		{
			lock (m_actionQueue)
			{
				if (m_actionQueue.Count == 0)
					return null;

				return m_actionQueue.Peek();
			}
		}

		#endregion
	}
}
