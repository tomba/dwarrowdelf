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

			SetStatus(JobState.Ok);
		}

		public void Abort()
		{
			SetStatus(JobState.Abort);
		}

		public void Fail()
		{
			SetStatus(JobState.Fail);
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

		public IAssignment CurrentSubJob { get; private set; }

		protected void SetAssignment(IAssignment assignment)
		{
			if (this.CurrentSubJob == assignment)
				return;

			if (this.CurrentSubJob != null)
			{
				this.CurrentSubJob.StateChanged -= OnSubJobStateChanged;
				if (this.CurrentSubJob.JobState == Jobs.JobState.Ok)
					this.CurrentSubJob.Abort();
			}

			this.CurrentSubJob = assignment;
			Notify("CurrentSubJob");

			if (this.CurrentSubJob != null)
			{
				this.CurrentSubJob.StateChanged += OnSubJobStateChanged;
				this.CurrentSubJob.Assign(this.Worker);
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

			this.Worker = worker;

			AssignOverride(worker);

			return this.JobState;
		}

		protected abstract void AssignOverride(ILiving worker);

		public JobState PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			while (true)
			{
				Debug.Assert(this.CurrentSubJob != null);

				this.CurrentSubJob.PrepareNextAction();
				Notify("CurrentAction");

				var state = this.JobState;

				switch (state)
				{
					case JobState.Ok:
						Debug.Assert(this.CurrentAction != null);
						return JobState.Ok;

					case JobState.Done:
						continue;

					case JobState.Abort:
					case JobState.Fail:
						Debug.Assert(this.CurrentAction == null);
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

			this.CurrentSubJob.ActionProgress(e);
			Notify("CurrentAction");

			return this.JobState;
		}

		protected void SetStatus(JobState state)
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
			if (this.StateChanged != null)
				StateChanged(this, state);
			Notify("JobState");
		}

		public event Action<IJob, JobState> StateChanged;

		void OnSubJobStateChanged(IJob job, JobState state)
		{
			Debug.Assert(job == this.CurrentSubJob);

			OnAssignmentStateChanged(state);
		}

		protected abstract void OnAssignmentStateChanged(JobState jobState);

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
