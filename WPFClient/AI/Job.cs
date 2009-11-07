using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame
{
	interface IJob
	{
		GameAction CurrentAction { get; }
		Living Worker { get; }

		Progress Assign(Living worker);
		void Quit();
		Progress PrepareNextAction();
		Progress ActionProgress(ActionProgressEvent e);
	}

	abstract class CompoundJob : IJob, INotifyPropertyChanged
	{
		Living m_worker;
		public Living Worker
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

		public GameAction CurrentAction
		{
			get { return this.CurrentTask.CurrentAction; }
		}

		public Progress Assign(Living worker)
		{
			if (this.Worker != null)
				throw new Exception();

			var res = AssignOverride(worker);
			if (res != Progress.Ok)
				return res;

			this.Worker = worker;

			return Progress.Ok;
		}

		protected abstract Progress AssignOverride(Living worker);

		public Progress PrepareNextAction()
		{
			while (true)
			{
				if (m_tasks.Count == 0)
				{
					Quit();
					return Progress.Done;
				}

				if (this.CurrentTask == null)
					this.CurrentTask = m_tasks.Dequeue();

				var res = this.CurrentTask.PrepareNextAction();

				switch (res)
				{
					case Progress.Ok:
						return Progress.Ok;

					case Progress.Done:
						this.CurrentTask = null;
						continue;

					case Progress.Abort:
					case Progress.Fail:
						Quit();
						return res;

					case Progress.None:
						throw new Exception();

					default:
						throw new Exception();
				}
			}
		}

		public void Quit()
		{
			if (this.Worker == null)
				throw new Exception();

			foreach (Task t in m_tasks)
				t.Quit();
			m_tasks.Clear();

			QuitOverride();

			this.Worker = null;
		}

		protected abstract void QuitOverride();

		public Progress ActionProgress(ActionProgressEvent e)
		{
			if (this.Worker == null)
				throw new Exception();

			if (this.CurrentTask == null)
				return Progress.None;

			var progress = this.CurrentTask.ActionProgress(e);

			switch (progress)
			{
				case Progress.None:
					return Progress.None;

				case Progress.Ok:
					return Progress.Ok;

				case Progress.Abort:
				case Progress.Fail:
					MyDebug.WriteLine("[AI] Task failed, cancel job");
					Quit();
					return progress;

				case Progress.Done:
					if (m_tasks.Count == 0)
					{
						Quit();
						return Progress.Done;
					}
					else
					{
						this.CurrentTask = null;
						return Progress.Ok;
					}

				default:
					throw new Exception();
			}
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

	class MineJob : CompoundJob
	{
		public Environment m_environment;
		public IntPoint3D m_location;

		public MineJob(Environment env, IntPoint3D location)
		{
			m_environment = env;
			m_location = location;
		}

		protected override Progress AssignOverride(Living worker)
		{
			if (worker.Environment != m_environment)
				return Progress.Abort;

			var floor = m_environment.World.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			if (m_environment.GetTerrainID(m_location) == floor.ID)
				return Progress.Done;

			m_tasks.Enqueue(new MoveTask(this, m_environment, m_location, true));
			m_tasks.Enqueue(new MineTask(this, m_environment, m_location));

			return Progress.Ok;
		}

		protected override void QuitOverride()
		{
		}

		public override string ToString()
		{
			return String.Format("MineJob");
		}
	}
}
