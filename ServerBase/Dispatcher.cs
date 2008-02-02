using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace MyGame
{
	class Dispatcher
	{
		World m_world;
		Queue<GameAction> m_actionQueue = new Queue<GameAction>();
		bool m_workerDequeuing = false;
		Action<GameAction> m_performCallback;

		public Dispatcher(World world, Action<GameAction> performCallback)
		{
			m_world = world;
			m_performCallback = performCallback;
			
		}

		public void EnqueueAction(GameAction action)
		{
			lock (m_actionQueue)
			{
				m_actionQueue.Enqueue(action);

				if (m_workerDequeuing == false)
				{
					ThreadPool.QueueUserWorkItem(PerformActions, m_world);
					m_workerDequeuing = true;
				}
			}
		}

		void PerformActions(object data)
		{
			World.CurrentWorld = m_world;

			try
			{
				while (true)
				{
					GameAction action;

					lock (m_actionQueue)
					{
						if (m_actionQueue.Count == 0)
						{
							m_world.SendChanges();
							m_workerDequeuing = false;
							break;
						}

						action = m_actionQueue.Dequeue();
					}

					m_performCallback(action);
				}
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("ERROR in dispatcher: {0}", e.Message);
				throw;
			}
		}
	}
}
