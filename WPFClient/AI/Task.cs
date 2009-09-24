using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame
{
	abstract class Task : INotifyPropertyChanged
	{
		protected Job m_job;
		protected GameAction m_currentAction;

		protected Task(Job job)
		{
			m_job = job;
		}

		public abstract Progress ActionProgress(ActionProgressEvent e);
		public abstract Progress Prepare();

		public abstract GameAction Do();

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	class MoveTask : Task
	{
		Queue<Direction> m_pathDirs;
		IntPoint3D m_dest;
		bool m_adjacent;

		public MoveTask(Job job, IntPoint3D destination, bool adjacent)
			: base(job)
		{
			m_dest = destination;
			m_adjacent = adjacent;
		}

		public override Progress Prepare()
		{
			var v = m_dest - m_job.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;

			// ZZZ only 2D
			int z = m_dest.Z;
			var src2d = m_job.Worker.Location2D;
			var dest2d = new IntPoint(m_dest.X, m_dest.Y);
			var env = m_job.Environment;
			var dirs = AStar.FindPath(src2d, dest2d, !m_adjacent,
				l => env.IsWalkable(new IntPoint3D(l, z)));

			m_pathDirs = new Queue<Direction>(dirs);

			if (m_pathDirs.Count == 0)
				return Progress.Fail;

			return Progress.Ok;
		}

		public override GameAction Do()
		{
			Debug.Assert(m_currentAction == null);

			var dir = GetNext();
			m_currentAction = new MoveAction(dir);
			return m_currentAction;
		}

		public override Progress ActionProgress(ActionProgressEvent e)
		{
			Debug.Assert(m_currentAction != null);

			if (e.Success == false)
			{
				m_currentAction = null;
				return Progress.Fail;
			}

			if (e.TurnsLeft > 0)
				return Progress.Ok;

			m_currentAction = null;

			var v = m_dest - m_job.Worker.Location;

			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;

			return Progress.Ok;
		}

		Direction GetNext()
		{
			if (m_pathDirs.Count == 0)
				return Direction.None;

			Direction dir = m_pathDirs.Dequeue();
			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			return dir;
		}

		public override string ToString()
		{
			return String.Format("MoveTask, num dirs {0}",
				m_pathDirs != null ? m_pathDirs.Count : 0);
		}
	}

	class MineTask : Task
	{
		IntPoint3D m_location;

		public MineTask(Job job, IntPoint3D location)
			: base(job)
		{
			m_location = location;
		}

		public override Progress Prepare()
		{
			Debug.Assert(m_currentAction == null);

			var v = m_location - m_job.Worker.Location;
			if (v.IsAdjacent2D)
				return Progress.Ok;
			else
				return Progress.Fail;
		}

		public override GameAction Do()
		{
			Debug.Assert(m_currentAction == null);

			MyDebug.WriteLine("mine!");

			var v = m_location - m_job.Worker.Location;
			m_currentAction = new MineAction(v.ToDirection());
			return m_currentAction;
		}

		public override Progress ActionProgress(ActionProgressEvent e)
		{
			Debug.Assert(m_currentAction != null);

			if (e.Success == false)
			{
				m_currentAction = null;
				return Progress.Fail;
			}

			if (e.TurnsLeft > 0)
				return Progress.Ok;

			m_currentAction = null;

			return Progress.Done;
		}

		public override string ToString()
		{
			return String.Format("MineTask");
		}
	}
}
