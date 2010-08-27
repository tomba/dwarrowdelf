using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame.Client
{
	abstract class SerialActionJob : IActionJob
	{
		IActionJob m_currentSubJob;
		ObservableCollection<IActionJob> m_subJobs;
		ReadOnlyObservableCollection<IActionJob> m_roSubJobs;

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			MyDebug.WriteLine("[AI S] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected SerialActionJob(IJob parent)
		{
			this.Parent = parent;
			m_subJobs = new ObservableCollection<IActionJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IActionJob>(m_subJobs);
		}

		public IJob Parent { get; private set; }

		Progress m_progress;
		public Progress Progress
		{
			get { return m_progress; }
			private set { m_progress = value; Notify("Progress"); }
		}

		public ReadOnlyObservableCollection<IActionJob> SubJobs { get { return m_roSubJobs; } }

		protected void AddSubJob(IActionJob job)
		{
			m_subJobs.Add(job);
		}

		IWorker m_worker;
		public IWorker Worker
		{
			get { return m_worker; }
			private set { m_worker = value; Notify("Worker"); }
		}

		public GameAction CurrentAction
		{
			get { return m_currentSubJob != null ? m_currentSubJob.CurrentAction : null; }
		}


		public void Abort()
		{
			foreach (var job in m_subJobs)
				job.Abort();

			SetProgress(Progress.Abort);
		}


		public Progress Assign(IWorker worker)
		{
			Debug.Assert(this.Worker == null);
			Debug.Assert(this.Progress == Progress.None || this.Progress == Progress.Abort);

			var progress = AssignOverride(worker);
			SetProgress(progress);
			if (progress != Progress.Ok)
				return progress;

			this.Worker = worker;

			m_currentSubJob = FindAndAssignJob(this.SubJobs, this.Worker, out progress);
			SetProgress(progress);
			return progress;
		}

		protected virtual Progress AssignOverride(IWorker worker)
		{
			return Progress.Ok;
		}



		public Progress PrepareNextAction()
		{
			var progress = DoPrepareNextAction();
			SetProgress(progress);
			return progress;
		}

		Progress DoPrepareNextAction()
		{
			while (true)
			{
				Progress progress;

				if (m_currentSubJob == null)
				{
					m_currentSubJob = FindAndAssignJob(this.SubJobs, this.Worker, out progress);
					if (progress != Progress.Ok)
						return progress;
				}

				progress = m_currentSubJob.PrepareNextAction();
				Notify("CurrentAction");

				switch (progress)
				{
					case Progress.Ok:
						return Progress.Ok;

					case Progress.Done:
						m_currentSubJob = null;
						continue;

					case Progress.Abort:
					case Progress.Fail:
						return progress;

					case Progress.None:
					default:
						throw new Exception();
				}
			}
		}



		public Progress ActionProgress(ActionProgressEvent e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Progress == Progress.Ok);
			Debug.Assert(m_currentSubJob != null);

			var progress = DoActionProgress(e);
			SetProgress(progress);
			return progress;
		}

		Progress DoActionProgress(ActionProgressEvent e)
		{
			var progress = m_currentSubJob.ActionProgress(e);
			Notify("CurrentAction");

			switch (progress)
			{
				case Progress.Ok:
					return Progress.Ok;

				case Progress.Abort:
				case Progress.Fail:
					return progress;

				case Progress.Done:
					m_currentSubJob = null;
					if (this.SubJobs.All(j => j.Progress == Progress.Done))
						return Progress.Done;
					else
						return Progress.Ok;

				case Progress.None:
				default:
					throw new Exception();
			}
		}

		static IActionJob FindAndAssignJob(IEnumerable<IActionJob> jobs, IWorker worker, out Progress progress)
		{
			Debug.Assert(!jobs.Any(j => j.Progress == Progress.Fail || j.Progress == Progress.Ok));

			//D("looking for new job");

			foreach (var job in jobs)
			{
				if (job.Progress == Progress.Done)
					continue;

				var subProgress = job.Assign(worker);

				switch (subProgress)
				{
					case Progress.Ok:
						//D("new job found");
						progress = subProgress;
						return job;

					case Progress.Done:
						continue;

					case Progress.Abort:
					case Progress.Fail:
						progress = subProgress;
						return null;

					case Progress.None:
					default:
						throw new Exception();
				}
			}

			// All subjobs are done

			//D("all subjobs done");

			progress = Progress.Done;
			return null;
		}


		protected void SetProgress(Progress progress)
		{
			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					break;

				case Progress.Done:
					Cleanup();
					this.Worker = null;
					m_currentSubJob = null;
					break;

				case Progress.Abort:
					this.Worker = null;
					m_currentSubJob = null;
					break;

				case Progress.Fail:
					Cleanup();
					this.Worker = null;
					m_currentSubJob = null;
					break;
			}

			this.Progress = progress;
		}

		protected virtual void Cleanup()
		{
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
}
