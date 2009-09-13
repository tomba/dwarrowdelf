using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class PetActor : IActor
	{
		Living m_object;
		Living m_player;
		Random m_random = new Random();

		public PetActor(Living ob, Living player)
		{
			m_object = ob;
			m_player = player;
		}

		GameAction GetNewAction()
		{
			return GetNewActionAstar();
		}

		Queue<Direction> m_pathDirs;
		IntPoint3D m_pathDest;

		GameAction GetNewActionAstar()
		{
			GameAction action;

			var v = m_player.Location - m_object.Location;

			if (v.ManhattanLength < 5)
				return new WaitAction(0, m_object, 1);

			if (m_pathDirs == null || (m_player.Location - m_pathDest).ManhattanLength > 3)
			{
				// ZZZ only 2D
				int z = m_player.Z;
				IEnumerable<Direction> dirs = AStar.FindPath(m_object.Location2D, m_player.Location2D,
					l => m_object.Environment.Bounds.Contains(new IntPoint3D(l, z)) && 
						m_object.Environment.IsWalkable(new IntPoint3D(l, z)));

				m_pathDirs = new Queue<Direction>(dirs);
				m_pathDest = m_player.Location;
			}

			if (m_pathDirs.Count == 0)
				return new WaitAction(0, m_object, 1);

			Direction dir = m_pathDirs.Dequeue();
			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			action = new MoveAction(0, m_object, dir);

			return action;
		}

		GameAction GetNewActionNoAstar()
		{
			GameAction action;

			var v = m_player.Location - m_object.Location;

			if (v.ManhattanLength < 3)
				return new WaitAction(0, m_object, 1);

			v.Normalize();

			if (v == new IntVector3D())
				return new WaitAction(0, m_object, 1);

			var env = m_object.Environment;

			action = null;
			int angle = 45;
			IntVector3D ov = v;
			for (int i = 0; i < 8; ++i)
			{
				v = ov;
				// 0, 45, -45, 90, -90, 135, -135, 180
				angle = ((i + 1) / 2) * (i % 2 * 2 - 1);
				v.FastRotate(angle);

				if (env.IsWalkable(m_object.Location + v))
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

		public bool IsInteractive
		{
			get { return false; }
		}

		public void DetermineAction()
		{
			if (m_object.HasAction)
				return;
			var a = GetNewAction();
			m_object.EnqueueAction(a);
		}

		#endregion
	}
}
