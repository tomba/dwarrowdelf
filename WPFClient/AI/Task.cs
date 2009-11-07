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
		public IJob Job { get; private set; }
		public Living Worker { get { return this.Job.Worker; } }
		GameAction m_currentAction;

		protected Task(IJob job)
		{
			this.Job = job;
		}

		public GameAction CurrentAction
		{
			get { return m_currentAction; }
			protected set { m_currentAction = value; }
		}

		public void Quit()
		{
		}

		public abstract Progress PrepareNextAction();

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

			var progress = CheckProgress();

			return progress;
		}

		protected abstract Progress CheckProgress();

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
		Environment m_environment;
		IntPoint3D m_dest;
		bool m_adjacent;
		IntPoint3D m_supposedLocation;

		public MoveTask(IJob job, Environment environment, IntPoint3D destination, bool adjacent)
			: base(job)
		{
			m_environment = environment;
			m_dest = destination;
			m_adjacent = adjacent;
		}

		public override Progress PrepareNextAction()
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var v = m_dest - this.Worker.Location;
				if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
					return Progress.Done;

				// ZZZ only 2D
				int z = m_dest.Z;
				var src2d = this.Worker.Location2D;
				var dest2d = new IntPoint(m_dest.X, m_dest.Y);
				var env = m_environment;
				var dirs = AStar.FindPath(src2d, dest2d, !m_adjacent,
					l => env.IsWalkable(new IntPoint3D(l, z)));

				m_pathDirs = new Queue<Direction>(dirs);

				if (m_pathDirs.Count == 0)
				{
					m_pathDirs = null;
					return Progress.Fail;
				}

				m_supposedLocation = this.Worker.Location;
			}

			Direction dir = m_pathDirs.Dequeue();

			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			var action = new MoveAction(dir);
			m_supposedLocation += IntVector3D.FromDirection(dir);

			this.CurrentAction = action;

			return Progress.Ok;
		}

		protected override Progress CheckProgress()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;
			else
				return Progress.Ok;
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
		Environment m_environment;

		public MineTask(IJob job, Environment environment, IntPoint3D location)
			: base(job)
		{
			m_environment = environment;
			m_location = location;
		}

		public override Progress PrepareNextAction()
		{
			var v = m_location - this.Worker.Location;

			if (!v.IsAdjacent2D)
				return Progress.Fail;

			if (CheckProgress() == Progress.Done)
				return Progress.Done;

			var action = new MineAction(v.ToDirection());

			this.CurrentAction = action;

			return Progress.Ok;
		}

		protected override Progress CheckProgress()
		{
			var floor = m_environment.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			if (m_environment.GetTerrainID(m_location) == floor.ID)
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return String.Format("MineTask");
		}
	}
}
