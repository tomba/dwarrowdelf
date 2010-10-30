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
			Debug.Print("[AI O] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		IEnumerator<IAssignment> m_enumerator;

		protected AssignmentGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
		}

		public JobType JobType { get { return JobType.Assignment; } }
		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }
		public JobState JobState { get; private set; }

		public void Retry()
		{
			Debug.Assert(this.JobState != JobState.Ok);
			Debug.Assert(this.CurrentSubJob == null);

			SetState(JobState.Ok);
		}

		public void Abort()
		{
			SetState(JobState.Abort);
		}

		public void Fail()
		{
			SetState(JobState.Fail);
		}

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.JobState == Jobs.JobState.Ok);
				return m_worker != null;
			}
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
			private set
			{
				if (m_currentSubJob == value) return;

				if (m_currentSubJob != null)
					m_currentSubJob.StateChanged -= OnSubJobStateChanged;

				m_currentSubJob = value;

				if (m_currentSubJob != null)
					m_currentSubJob.StateChanged += OnSubJobStateChanged;

				Notify("CurrentSubJob");
			}
		}

		public GameAction CurrentAction
		{
			get { return this.CurrentSubJob != null ? this.CurrentSubJob.CurrentAction : null; }
		}

		public JobState Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.JobState == JobState.Ok);

			D("Assign {0}", worker);

			var state = AssignOverride(worker);
			SetState(state);
			if (state != JobState.Ok)
				return state;

			this.Worker = worker;

			m_enumerator = GetAssignmentEnumerator();

			this.CurrentSubJob = FindAndAssignJob(out state);
			SetState(state);
			return state;
		}

		protected abstract IEnumerator<IAssignment> GetAssignmentEnumerator();

		protected virtual JobState AssignOverride(ILiving worker)
		{
			return JobState.Ok;
		}


		public JobState PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			var state = DoPrepareNextAction();
			SetState(state);
			return state;
		}

		JobState DoPrepareNextAction()
		{
			while (true)
			{
				JobState state;

				if (this.CurrentSubJob == null)
				{
					this.CurrentSubJob = FindAndAssignJob(out state);
					if (state != JobState.Ok)
						return state;
				}

				state = this.CurrentSubJob.PrepareNextAction();
				Notify("CurrentAction");

				switch (state)
				{
					case JobState.Ok:
						return JobState.Ok;

					case JobState.Done:
						this.CurrentSubJob = null;
						continue;

					case JobState.Abort:
					case JobState.Fail:
						return state;
				}
			}
		}



		public JobState ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobState == JobState.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentSubJob != null);

			D("ActionProgress");

			var state = DoActionProgress(e);
			SetState(state);
			return state;
		}

		JobState DoActionProgress(ActionProgressChange e)
		{
			var state = this.CurrentSubJob.ActionProgress(e);
			Notify("CurrentAction");

			switch (state)
			{
				case JobState.Ok:
					return JobState.Ok;

				case JobState.Abort:
				case JobState.Fail:
					return state;

				case JobState.Done:
					this.CurrentSubJob = null;
					return CheckProgress();

				default:
					throw new Exception();
			}
		}

		protected abstract JobState CheckProgress();

		IAssignment FindAndAssignJob(out JobState state)
		{
			D("looking for new job");

			while (true)
			{
				var ok = m_enumerator.MoveNext();

				if (!ok)
				{
					D("all subjobs done");
					state = JobState.Done;
					return null;
				}

				var job = m_enumerator.Current;

				var subState = job.Assign(this.Worker);

				switch (subState)
				{
					case JobState.Ok:
						//D("new job found");
						state = subState;
						return job;

					case JobState.Done:
						continue;

					case JobState.Abort:
					case JobState.Fail:
						state = subState;
						return null;

					default:
						throw new Exception();
				}
			}
		}


		void SetState(JobState state)
		{
			if (this.JobState == state)
				return;

			D("SetState({0})", state);

			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
					Debug.Assert(this.JobState == JobState.Ok);
					break;

				case JobState.Abort:
					Debug.Assert(this.JobState == JobState.Ok || this.JobState == JobState.Done);
					break;

				case JobState.Fail:
					Debug.Assert(this.JobState == JobState.Ok);
					break;
			}

			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
					this.Worker = null;
					this.CurrentSubJob = null;
					break;

				case JobState.Abort:
					if (this.CurrentSubJob != null && this.CurrentSubJob.JobState == JobState.Ok)
						this.CurrentSubJob.Abort();

					this.Worker = null;
					this.CurrentSubJob = null;
					break;

				case JobState.Fail:
					if (this.CurrentSubJob != null && this.CurrentSubJob.JobState == JobState.Ok)
						this.CurrentSubJob.Fail();

					this.Worker = null;
					this.CurrentSubJob = null;
					break;
			}

			this.JobState = state;
			OnStateChanged(state);
			if (this.StateChanged != null)
				StateChanged(this, state);
			Notify("JobState");
		}

		public event Action<IJob, JobState> StateChanged;

		protected virtual void OnStateChanged(JobState state) { }

		void OnSubJobStateChanged(IJob job, JobState state)
		{
			Debug.Assert(job == this.CurrentSubJob);

			if (state == JobState.Ok || state == JobState.Done)
				return;

			// else Abort or Fail
			this.Worker = null;
			this.CurrentSubJob = null;

			SetState(state);
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
