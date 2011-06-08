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

		public JobType JobType { get { return Dwarrowdelf.Jobs.JobType.Assignment; } }
		[GameProperty]
		public IJob Parent { get; private set; }
		[GameProperty]
		public ActionPriority Priority { get; private set; }
		[GameProperty]
		public JobStatus JobStatus { get; private set; }

		public void Retry()
		{
			Debug.Assert(this.JobStatus != JobStatus.Ok);
			Debug.Assert(this.CurrentAssignment == null);

			SetStatus(JobStatus.Ok);
		}

		public void Abort()
		{
			SetStatus(JobStatus.Abort);
		}

		public void Fail()
		{
			SetStatus(JobStatus.Fail);
		}

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.JobStatus == Jobs.JobStatus.Ok);
				return m_worker != null;
			}
		}

		ILiving m_worker;
		[GameProperty]
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		IAssignment m_assignment;
		[GameProperty]
		public IAssignment CurrentAssignment
		{
			get { return m_assignment; }

			private set
			{
				if (m_assignment == value)
					return;

				if (m_assignment != null)
				{
					Debug.Assert(m_assignment.JobStatus != Jobs.JobStatus.Ok);
					m_assignment.StatusChanged -= OnCurrentAssignmentStatusChanged;
				}

				m_assignment = value;
				Notify("CurrentAssignment");

				if (m_assignment != null)
				{
					m_assignment.StatusChanged += OnCurrentAssignmentStatusChanged;
				}
			}
		}

		public GameAction CurrentAction
		{
			get { return this.CurrentAssignment != null ? this.CurrentAssignment.CurrentAction : null; }
		}

		public JobStatus Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.JobStatus == JobStatus.Ok);

			D("Assign {0}", worker);

			this.Worker = worker;

			var status = AssignOverride(worker);

			SetStatus(status);

			return this.JobStatus;
		}

		protected abstract JobStatus AssignOverride(ILiving worker);

		public JobStatus PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			while (true)
			{
				while (this.CurrentAssignment == null || this.CurrentAssignment.JobStatus != Jobs.JobStatus.Ok)
				{
					var assignment = PrepareNextAssignment();

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					Debug.Assert(assignment.JobStatus == Jobs.JobStatus.Ok);

					this.CurrentAssignment = assignment;

					var status = this.CurrentAssignment.Assign(this.Worker);

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					if (status != Jobs.JobStatus.Ok)
						continue;
				}

				Debug.Assert(this.CurrentAssignment != null);
				Debug.Assert(this.CurrentAssignment.JobStatus == Jobs.JobStatus.Ok);

				{
					var status = this.CurrentAssignment.PrepareNextAction();
					Notify("CurrentAction");

					if (this.JobStatus != Jobs.JobStatus.Ok)
						return this.JobStatus;

					switch (status)
					{
						case JobStatus.Ok:
							Debug.Assert(this.CurrentAction != null);
							return JobStatus.Ok;

						case JobStatus.Done:
							continue;

						case JobStatus.Abort:
						case JobStatus.Fail:
							Debug.Assert(this.CurrentAction == null);
							continue;
					}
				}
			}
		}

		protected abstract IAssignment PrepareNextAssignment();

		public JobStatus ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.JobStatus == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentAssignment != null);

			D("ActionProgress");

			this.CurrentAssignment.ActionProgress(e);
			Notify("CurrentAction");

			return this.JobStatus;
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.JobStatus == status)
				return;

			D("SetState({0})", status);

			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
					Debug.Assert(this.JobStatus == JobStatus.Ok);
					break;

				case JobStatus.Abort:
					Debug.Assert(this.JobStatus == JobStatus.Ok || this.JobStatus == JobStatus.Done);
					break;

				case JobStatus.Fail:
					Debug.Assert(this.JobStatus == JobStatus.Ok);
					break;
			}

			this.JobStatus = status;

			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
					this.Worker = null;
					this.CurrentAssignment = null;
					break;

				case JobStatus.Abort:
					if (this.CurrentAssignment != null && this.CurrentAssignment.JobStatus == JobStatus.Ok)
						this.CurrentAssignment.Abort();

					this.Worker = null;
					this.CurrentAssignment = null;
					break;

				case JobStatus.Fail:
					if (this.CurrentAssignment != null && this.CurrentAssignment.JobStatus == JobStatus.Ok)
						this.CurrentAssignment.Fail();

					this.Worker = null;
					this.CurrentAssignment = null;
					break;
			}

			if (this.StatusChanged != null)
				StatusChanged(this, status);
			Notify("JobStatus");
		}

		public event Action<IJob, JobStatus> StatusChanged;

		void OnCurrentAssignmentStatusChanged(IJob job, JobStatus status)
		{
			Debug.Assert(job == this.CurrentAssignment);

			OnAssignmentStateChanged(status);
		}

		protected abstract void OnAssignmentStateChanged(JobStatus jobState);

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
