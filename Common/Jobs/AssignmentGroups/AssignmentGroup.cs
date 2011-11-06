using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public abstract class AssignmentGroup : IAssignment, IJobObserver
	{
		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[AI O] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected AssignmentGroup(IJobObserver parent)
		{
			this.Parent = parent;
			this.Status = JobStatus.Ok;
		}

		protected AssignmentGroup(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public IJobObserver Parent { get; private set; }
		[SaveGameProperty]
		public JobStatus Status { get; private set; }

		public void Abort()
		{
			SetStatus(JobStatus.Abort);
		}

		public bool IsAssigned
		{
			get
			{
				Debug.Assert(m_worker == null || this.Status == JobStatus.Ok);
				return m_worker != null;
			}
		}

		ILiving m_worker;
		[SaveGameProperty]
		public ILiving Worker
		{
			get { return m_worker; }
			private set { if (m_worker == value) return; m_worker = value; Notify("Worker"); }
		}

		IAssignment m_assignment;
		[SaveGameProperty]
		public IAssignment CurrentAssignment
		{
			get { return m_assignment; }

			private set
			{
				if (m_assignment == value)
					return;

				if (m_assignment != null)
				{
					if (m_assignment.Status == JobStatus.Ok)
						m_assignment.Abort();
				}

				m_assignment = value;
				Notify("CurrentAssignment");
			}
		}

		public GameAction CurrentAction
		{
			get { return this.CurrentAssignment != null ? this.CurrentAssignment.CurrentAction : null; }
		}

		public void Assign(ILiving worker)
		{
			Debug.Assert(this.IsAssigned == false);
			Debug.Assert(this.Status == JobStatus.Ok);

			D("Assign {0}", worker);

			this.Worker = worker;

			AssignOverride(worker);

			Debug.Assert(this.Status == JobStatus.Ok);
		}

		protected virtual void AssignOverride(ILiving worker)
		{
		}

		public JobStatus PrepareNextAction()
		{
			Debug.Assert(this.CurrentAction == null);

			D("PrepareNextAction");

			while (true)
			{
				while (this.CurrentAssignment == null || this.CurrentAssignment.Status != JobStatus.Ok)
				{
					var assignment = PrepareNextAssignment();

					if (this.Status != JobStatus.Ok)
						return this.Status;

					Debug.Assert(assignment.Status == JobStatus.Ok);

					this.CurrentAssignment = assignment;

					this.CurrentAssignment.Assign(this.Worker);
					Debug.Assert(this.CurrentAssignment.Status == JobStatus.Ok);
				}

				Debug.Assert(this.CurrentAssignment != null);
				Debug.Assert(this.CurrentAssignment.Status == JobStatus.Ok);

				{
					var status = this.CurrentAssignment.PrepareNextAction();
					Notify("CurrentAction");

					if (this.Status != JobStatus.Ok)
						return this.Status;

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

		public JobStatus ActionProgress()
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Status == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentAssignment != null);

			D("ActionProgress");

			this.CurrentAssignment.ActionProgress();
			Notify("CurrentAction");

			return this.Status;
		}

		public JobStatus ActionDone(ActionState actionStatus)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Status == JobStatus.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(this.CurrentAssignment != null);

			D("ActionProgress");

			this.CurrentAssignment.ActionDone(actionStatus);
			Notify("CurrentAction");

			return this.Status;
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.Status == status)
				return;

			D("SetState({0})", status);

			if (status == JobStatus.Ok)
				throw new Exception();

			if (this.Status != JobStatus.Ok)
				throw new Exception();

			this.Status = status;

			this.Worker = null;
			this.CurrentAssignment = null;

			OnStatusChanged(status);

			if (this.Parent != null)
				this.Parent.OnObservableJobStatusChanged(this, status);

			if (this.StatusChanged != null)
				StatusChanged(this, status);

			Notify("JobStatus");
		}

		protected virtual void OnStatusChanged(JobStatus status) { }

		public event Action<IJob, JobStatus> StatusChanged;


		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			Debug.Assert(job == this.CurrentAssignment);

			switch (status)
			{
				case JobStatus.Ok:
					throw new Exception();

				case JobStatus.Abort:
					OnAssignmentAborted();
					break;

				case JobStatus.Fail:
					OnAssignmentFailed();
					break;

				case JobStatus.Done:
					OnAssignmentDone();
					break;
			}
		}

		protected virtual void OnAssignmentAborted()
		{
			SetStatus(JobStatus.Abort);
		}

		protected virtual void OnAssignmentFailed()
		{
			SetStatus(JobStatus.Fail);
		}

		protected virtual void OnAssignmentDone()
		{
			SetStatus(JobStatus.Done);
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
