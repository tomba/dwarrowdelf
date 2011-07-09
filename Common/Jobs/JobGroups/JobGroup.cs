using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public abstract class JobGroup : IJobGroup
	{
		ObservableCollection<IJob> m_subJobs;
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;

			m_subJobs = new ObservableCollection<IJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);
		}

		protected void AddSubJob(IJob job)
		{
			m_subJobs.Add(job);
			job.StatusChanged += OnSubJobStatusChangedInternal;
		}

		protected void AddSubJobs(IEnumerable<IJob> jobs)
		{
			foreach (var job in jobs)
				AddSubJob(job);
		}

		protected void RemoveSubJob(IJob job)
		{
			Debug.Assert(m_subJobs.Contains(job));

			job.StatusChanged -= OnSubJobStatusChangedInternal;
			m_subJobs.Remove(job);
		}

		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }
		public JobStatus JobStatus { get; private set; }

		public event Action<IJob, JobStatus> StatusChanged;

		public void Retry()
		{
			throw new Exception();
			//foreach (var job in m_subJobs.Where(j => j.JobStatus == Jobs.JobStatus.Abort))
			//	job.Retry();
		}

		public void Abort()
		{
			throw new Exception();
			//foreach (var job in m_subJobs.Where(j => j.JobStatus == Jobs.JobStatus.Ok))
			//	job.Abort();
		}

		public void Fail()
		{
			throw new Exception();
			//foreach (var job in m_subJobs)
			//	job.Fail();
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public IEnumerable<IAssignment> GetAssignments(ILiving living)
		{
			foreach (var job in GetJobs(living))
				foreach (var a in job.GetAssignments(living))
					yield return a;
		}

		protected virtual IEnumerable<IJob> GetJobs(ILiving living)
		{
			return this.SubJobs.Where(j => j.JobStatus == JobStatus.Ok);
		}

		void OnSubJobStatusChangedInternal(IJob job, JobStatus status)
		{
			switch (status)
			{
				case Jobs.JobStatus.Ok:
					throw new Exception();

				case Jobs.JobStatus.Abort:
					OnSubJobAborted(job);
					break;

				case Jobs.JobStatus.Fail:
					OnSubJobFailed(job);
					break;

				case Jobs.JobStatus.Done:
					OnSubJobDone(job);
					break;
			}
		}

		protected virtual void OnSubJobAborted(IJob job)
		{
			SetStatus(Jobs.JobStatus.Abort);
		}

		protected virtual void OnSubJobFailed(IJob job)
		{
			SetStatus(Jobs.JobStatus.Fail);
		}

		protected virtual void OnSubJobDone(IJob job)
		{
			SetStatus(Jobs.JobStatus.Done);
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.JobStatus == status)
				return;

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
					m_subJobs.Clear();
					m_subJobs = null;
					m_roSubJobs = null;
					break;

				case JobStatus.Abort:
					// XXX Abort others?

					m_subJobs.Clear();
					m_subJobs = null;
					m_roSubJobs = null;
					break;

				case JobStatus.Fail:
					// XXX Fail others?

					m_subJobs.Clear();
					m_subJobs = null;
					m_roSubJobs = null;
					break;
			}

			if (this.StatusChanged != null)
				StatusChanged(this, status);
			Notify("JobStatus");
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
