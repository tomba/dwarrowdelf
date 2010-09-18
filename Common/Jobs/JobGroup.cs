using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Dwarrowdelf.Jobs
{
	public abstract class JobGroup : IJobGroup
	{
		ObservableCollection<IJob> m_subJobs = new ObservableCollection<IJob>();
		ReadOnlyObservableCollection<IJob> m_roSubJobs;

		protected JobGroup(IJob parent, ActionPriority priority)
		{
			this.Parent = parent;
			this.Priority = priority;
			m_subJobs = new ObservableCollection<IJob>();
			m_roSubJobs = new ReadOnlyObservableCollection<IJob>(m_subJobs);
		}

		public IJob Parent { get; private set; }
		public ActionPriority Priority { get; private set; }

		public virtual Progress Progress
		{
			get
			{
				if (this.SubJobs.All(j => j.Progress == Progress.Done))
					return Progress.Done;

				if (this.SubJobs.Any(j => j.Progress == Progress.Fail))
					return Progress.Fail;

				return Progress.None;
			}
		}

		public void Abort()
		{
			foreach (var job in m_subJobs)
				job.Abort();
		}

		public ReadOnlyObservableCollection<IJob> SubJobs { get { return m_roSubJobs; } }

		public abstract JobGroupType JobGroupType { get; }

		protected void AddSubJob(IJob job)
		{
			m_subJobs.Add(job);
			job.PropertyChanged += SubJobPropertyChanged;
		}

		void SubJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Progress")
				Notify("Progress");
		}

		// XXX not called
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


	public abstract class ParallelJobGroup : JobGroup
	{
		protected ParallelJobGroup(IJob parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		public override Progress Progress
		{
			get
			{
				var progress = base.Progress;

				if (progress != Progress.None)
					return progress;

				if (this.SubJobs.All(j => j.Progress == Progress.Ok || j.Progress == Progress.Done))
					return Progress.Ok;

				return Progress.None;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Parallel; } }
	}


	public abstract class SerialJobGroup : JobGroup
	{
		protected SerialJobGroup(IJobGroup parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		public override Progress Progress
		{
			get
			{
				if (this.SubJobs.Any(j => j.Progress == Progress.Ok))
					return Progress.Ok;

				return base.Progress;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Serial; } }
	}


}
