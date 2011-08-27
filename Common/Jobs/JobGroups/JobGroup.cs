using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObject(UseRef = true)]
	public abstract class JobGroup : IJobGroup
	{
		[SaveGameProperty]
		ObservableCollection<IJob> m_subJobs;
		[SaveGameProperty]
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJob parent)
		{
			this.Parent = parent;

			m_subJobs = new ObservableCollection<IJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);

			this.Status = JobStatus.Ok;
		}

		protected JobGroup(SaveGameContext ctx)
		{
		}

		protected void AddSubJob(IJob job)
		{
			m_subJobs.Add(job);
			job.StatusChanged += OnSubJobStatusChangedInternal;

			if (job.Status != JobStatus.Ok)
			{
				// XXX I think job should be always Ok when it is constructed. Init() method for jobs?
				OnSubJobStatusChangedInternal(job, job.Status);
				return;
			}
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

		void ClearSubJobs()
		{
			foreach (var job in m_subJobs)
			{
				job.StatusChanged -= OnSubJobStatusChangedInternal;

				if (job.Status == JobStatus.Ok)
					job.Abort();
			}

			m_subJobs.Clear();
		}

		[SaveGameProperty]
		public IJob Parent { get; private set; }
		[SaveGameProperty]
		public JobStatus Status { get; private set; }

		protected virtual void OnStatusChanged(JobStatus status) { }

		public event Action<IJob, JobStatus> StatusChanged;

		public void Abort()
		{
			SetStatus(JobStatus.Abort);
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public IAssignment FindAssignment(ILiving living)
		{
			foreach (var job in GetJobs(living))
			{
				var assignment = job as IAssignment;
				if (assignment != null)
				{
					if (assignment.IsAssigned == false)
						return assignment;
					else
						return null;
				}

				var jobGroup = (IJobGroup)job;
				assignment = jobGroup.FindAssignment(living);
				if (assignment != null)
					return assignment;
			}

			return null;
		}

		protected virtual IEnumerable<IJob> GetJobs(ILiving living)
		{
			return this.SubJobs.Where(j => j.Status == JobStatus.Ok);
		}

		void OnSubJobStatusChangedInternal(IJob job, JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Ok:
					throw new Exception();

				case JobStatus.Abort:
					OnSubJobAborted(job);
					break;

				case JobStatus.Fail:
					OnSubJobFailed(job);
					break;

				case JobStatus.Done:
					OnSubJobDone(job);
					break;
			}
		}

		protected virtual void OnSubJobAborted(IJob job)
		{
			SetStatus(JobStatus.Abort);
		}

		protected virtual void OnSubJobFailed(IJob job)
		{
			SetStatus(JobStatus.Fail);
		}

		protected virtual void OnSubJobDone(IJob job)
		{
			SetStatus(JobStatus.Done);
		}

		protected void SetStatus(JobStatus status)
		{
			if (this.Status == status)
				return;

			if (status == JobStatus.Ok)
				throw new Exception();

			this.Status = status;

			switch (status)
			{
				case JobStatus.Done:
					Debug.Assert(this.SubJobs.Count == 0);
					goto case JobStatus.Fail;

				case JobStatus.Abort:
				case JobStatus.Fail:
					ClearSubJobs();
					m_subJobs = null;
					m_roSubJobs = null;
					break;
			}

			OnStatusChanged(status);

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
