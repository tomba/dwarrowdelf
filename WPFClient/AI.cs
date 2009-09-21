using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class AI
	{
		ClientGameObject m_object;

		Queue<Direction> m_pathDirs;
		IntPoint3D m_pathDest;

		public AI(ClientGameObject ob)
		{
			m_object = ob;
		}

		public void ActionRequired()
		{
			if (m_object == GameData.Data.CurrentObject)
				return;

			var action = GetNewActionAstar(GameData.Data.CurrentObject);
			GameData.Data.Connection.DoAction(action);
		}

		GameAction GetNewActionAstar(ClientGameObject player)
		{
			GameAction action;
			var tid = GameData.Data.Connection.GetNewTransactionID();

			var v = player.Location - m_object.Location;

			if (v.ManhattanLength < 5)
				return new WaitAction(tid, m_object, 1);

			if (m_pathDirs == null || (player.Location - m_pathDest).ManhattanLength > 3)
			{
				// ZZZ only 2D
				int z = player.Location.Z;
				var env = m_object.Environment;
				var dirs = AStar.FindPath(m_object.Location2D, player.Location2D,
					l => env.IsWalkable(new IntPoint3D(l, z)));

				m_pathDirs = new Queue<Direction>(dirs);
				m_pathDest = player.Location;
			}

			if (m_pathDirs.Count == 0)
				return new WaitAction(tid, m_object, 1);

			Direction dir = m_pathDirs.Dequeue();
			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			action = new MoveAction(tid, m_object, dir);

			return action;
		}
	}
}
