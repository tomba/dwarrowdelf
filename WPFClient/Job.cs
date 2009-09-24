using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame
{
	public enum Progress
	{
		Ok,
		Fail,
		Done,
	}

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
			m_currentAction = new MoveAction(m_job.Worker, dir);
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
			m_currentAction = new MineAction(m_job.Worker, v.ToDirection());
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


	abstract class Job : INotifyPropertyChanged
	{
		public Environment Environment { get; protected set; }
		public IntPoint3D Location { get; protected set; }

		ClientGameObject m_worker;
		public ClientGameObject Worker
		{
			get { return m_worker; }
			protected set
			{
				m_worker = value;
				Notify("Worker");
			}
		}

		Task m_currentTask;
		public Task CurrentTask
		{
			get { return m_currentTask; }

			protected set
			{
				m_currentTask = value;
				Notify("CurrentTask");
			}
		}

		protected Queue<Task> m_tasks = new Queue<Task>();


		public GameAction Do()
		{
			return this.CurrentTask.Do();
		}

		protected abstract bool Prepare();

		public virtual void Cancel()
		{
			this.Worker = null;
		}

		public Progress Take(ClientGameObject worker)
		{
			if (this.Worker != null)
				throw new Exception();

			if (worker.Environment != this.Environment)
				return Progress.Fail;

			this.Worker = worker;

			Prepare();

			while (true)
			{
				if (m_tasks.Count == 0)
					return Progress.Done;

				this.CurrentTask = m_tasks.Dequeue();
				var res = this.CurrentTask.Prepare();

				if (res == Progress.Fail)
				{
					this.Worker = null;
					m_tasks.Clear();
					return Progress.Fail;
				}
				else if (res == Progress.Ok)
				{
					return Progress.Ok;
				}
			}
		}

		public Progress ActionProgress(ActionProgressEvent e)
		{
			if (e.Success == false)
			{
				MyDebug.WriteLine("JOB FAIL!!!");
				Cancel();
				return Progress.Fail;
			}

			if (e.TurnsLeft == 0)
			{
				var progress = this.CurrentTask.ActionProgress(e);
				if (progress == Progress.Done)
				{
					if (m_tasks.Count == 0)
					{
						this.CurrentTask = null;
						return Progress.Done;
					}

					this.CurrentTask = m_tasks.Dequeue();
				}
			}

			return Progress.Ok;
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	class MineJob : Job
	{
		public MineJob(Environment env, IntPoint3D location)
		{
			this.Environment = env;
			this.Location = location;
		}

		protected override bool Prepare()
		{
			m_tasks.Enqueue(new MoveTask(this, this.Location, true));
			m_tasks.Enqueue(new MineTask(this, this.Location));
			return true;
		}

		public override string ToString()
		{
			return String.Format("MineJob");
		}
	}
}
