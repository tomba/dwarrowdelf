using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class PetActor : IActor
	{
		ServerGameObject m_object;
		Living m_player;
		GameAction m_currentAction;
		Random m_random = new Random();

		public PetActor(ServerGameObject ob, Living player)
		{
			m_object = ob;
			m_player = player;
		}

		GameAction GetNewAction()
		{
			GameAction action;

			var v = m_player.Location - m_object.Location;

			if (v.Length < 2)
				return new WaitAction(0, m_object, 1);

			v.Normalize();

			if (v == new IntVector(0, 0))
				return new WaitAction(0, m_object, 1);

			var env = m_object.Environment;
			Terrains terrains = env.World.Terrains;

			action = null;
			int angle = 45;
			IntVector ov = v;
			for (int i = 0; i < 8; ++i)
			{
				v = ov;
				// 0, 45, -45, 90, -90, 135, -135, 180
				angle = 45 * ((i + 1) / 2) * (i % 2 * 2 - 1);
				v.Rotate(angle);

				if (terrains[env.GetTerrainID(m_object.Location + v)].IsWalkable)
				{
					Direction dir = v.ToDirection();
					if (dir == Direction.None)
						throw new Exception();
					action = new MoveAction(0, m_object, dir);
					break;
				}
			}

			if (action == null)
				return new WaitAction(0, m_object, 1);

			return action;
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

		public bool IsInteractive
		{
			get { return false; }
		}

		public void ReportAction(bool done, bool success)
		{
		}

		// Disable "event not used"
#pragma warning disable 67
		public event Action ActionQueuedEvent;

		#endregion
	}
}
