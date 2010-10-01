using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public abstract class AssignmentGroup : IAssignment
	{
		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[AI O] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		IEnumerator<IAssignment> m_enumerator;

		protected AssignmentGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }

		Progress m_progress;
		public Progress Progress
		{
			get { return m_progress; }
			private set { if (m_progress == value) return; m_progress = value; Notify("Progress"); }
		}

		ILiving m_worker;
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		IAssignment m_currentSubJob;
		public IAssignment CurrentSubJob
		{
			get { return m_currentSubJob; }
			private set { if (m_currentSubJob == value) return; m_currentSubJob = value; Notify("CurrentSubJob"); }
		}

		public GameAction CurrentAction
		{
			get { return this.CurrentSubJob != null ? this.CurrentSubJob.CurrentAction : null; }
		}


		public void Retry()
		{
			Debug.Assert(this.Progress == Jobs.Progress.Abort);
			Debug.Assert(this.CurrentSubJob == null);

			SetProgress(Progress.None);
		}

		public void Abort()
		{
			if (this.Progress != Jobs.Progress.Ok)
				return;

			if (this.CurrentSubJob != null)
				this.CurrentSubJob.Abort();

			AbortOverride();

			SetProgress(Progress.Abort);
		}

		protected virtual void AbortOverride() { }

		public Progress Assign(ILiving worker)
		{
			Debug.Assert(this.Worker == null);
			Debug.Assert(this.Progress == Progress.None || this.Progress == Progress.Abort);

			D("Assign {0}", worker);

			var progress = AssignOverride(worker);
			SetProgress(progress);
			if (progress != Progress.Ok)
				return progress;

			this.Worker = worker;

			m_enumerator = GetAssignmentEnumerator();

			this.CurrentSubJob = FindAndAssignJob(out progress);
			SetProgress(progress);
			return progress;
		}

		protected abstract IEnumerator<IAssignment> GetAssignmentEnumerator();

		protected virtual Progress AssignOverride(ILiving worker)
		{
			return Progress.Ok;
		}


		public Progress PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			var progress = DoPrepareNextAction();
			SetProgress(progress);
			return progress;
		}

		Progress DoPrepareNextAction()
		{
			while (true)
			{
				Progress progress;

				if (this.CurrentSubJob == null)
				{
					this.CurrentSubJob = FindAndAssignJob(out progress);
					if (progress != Progress.Ok)
						return progress;
				}

				progress = this.CurrentSubJob.PrepareNextAction();
				Notify("CurrentAction");

				switch (progress)
				{
					case Progress.Ok:
						return Progress.Ok;

					case Progress.Done:
						this.CurrentSubJob = null;
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



		public Progress ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Progress == Progress.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentSubJob != null);

			D("ActionProgress");

			var progress = DoActionProgress(e);
			SetProgress(progress);
			return progress;
		}

		Progress DoActionProgress(ActionProgressChange e)
		{
			var progress = this.CurrentSubJob.ActionProgress(e);
			Notify("CurrentAction");

			switch (progress)
			{
				case Progress.Ok:
					return Progress.Ok;

				case Progress.Abort:
				case Progress.Fail:
					return progress;

				case Progress.Done:
					this.CurrentSubJob = null;
					return CheckProgress();

				case Progress.None:
				default:
					throw new Exception();
			}
		}

		protected abstract Progress CheckProgress();

		IAssignment FindAndAssignJob(out Progress progress)
		{
			D("looking for new job");

			while (true)
			{
				var ok = m_enumerator.MoveNext();

				if (!ok)
				{
					D("all subjobs done");
					progress = Progress.Done;
					return null;
				}

				var job = m_enumerator.Current;

				var subProgress = job.Assign(this.Worker);

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
		}


		protected void SetProgress(Progress progress)
		{
			D("SetProgress({0})", progress);

			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					break;

				case Progress.Done:
					Cleanup();
					this.Worker = null;
					this.CurrentSubJob = null;
					break;

				case Progress.Abort:
					this.Worker = null;
					this.CurrentSubJob = null;
					break;

				case Progress.Fail:
					Cleanup();
					this.Worker = null;
					this.CurrentSubJob = null;
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
