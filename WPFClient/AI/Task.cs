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
		public Job Job { get; private set; }
		public Living Worker { get { return this.Job.Worker; } }
		GameAction m_currentAction;

		protected Task(Job job)
		{
			this.Job = job;
		}

		public abstract Progress Prepare();

		public GameAction ActionRequired()
		{
			Debug.Assert(m_currentAction == null);
			m_currentAction = GetNextAction();
			return m_currentAction;
		}

		public Progress ActionProgress(ActionProgressEvent e)
		{
			if (m_currentAction == null || e.TransactionID != m_currentAction.TransactionID)
				return Progress.None;

			if (e.Success == false)
			{
				m_currentAction = null;
				return Progress.Fail;
			}

			if (e.TurnsLeft > 0)
				return Progress.Ok;

			m_currentAction = null;

			var progress = CheckDone();
			return progress;
		}

		protected abstract GameAction GetNextAction();
		protected abstract Progress CheckDone();

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
		IntPoint3D m_supposedLocation;

		public MoveTask(Job job, IntPoint3D destination, bool adjacent)
			: base(job)
		{
			m_dest = destination;
			m_adjacent = adjacent;
		}

		public override Progress Prepare()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;

			// ZZZ only 2D
			int z = m_dest.Z;
			var src2d = this.Worker.Location2D;
			var dest2d = new IntPoint(m_dest.X, m_dest.Y);
			var env = this.Job.Environment;
			var dirs = AStar.FindPath(src2d, dest2d, !m_adjacent,
				l => env.IsWalkable(new IntPoint3D(l, z)));

			m_pathDirs = new Queue<Direction>(dirs);

			if (m_pathDirs.Count == 0)
				return Progress.Fail;

			m_supposedLocation = this.Worker.Location;

			return Progress.Ok;
		}

		protected override GameAction GetNextAction()
		{
			if (m_supposedLocation != this.Worker.Location)
				return null;

			var dir = GetNextDir();
			var action = new MoveAction(dir);
			m_supposedLocation += IntVector3D.FromDirection(dir);

			return action;
		}

		protected override Progress CheckDone()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;
			else
				return Progress.Ok;
		}

		Direction GetNextDir()
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
			var v = m_location - this.Worker.Location;
			if (v.IsAdjacent2D)
				return Progress.Ok;
			else
				return Progress.Fail;
		}

		protected override GameAction GetNextAction()
		{
			var v = m_location - this.Worker.Location;

			if (!v.IsAdjacent2D)
				return null;

			var action = new MineAction(v.ToDirection());
			return action;
		}

		protected override Progress CheckDone()
		{
			return Progress.Done;
		}

		public override string ToString()
		{
			return String.Format("MineTask");
		}
	}
}
