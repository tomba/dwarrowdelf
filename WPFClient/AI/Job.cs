using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame
{
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
