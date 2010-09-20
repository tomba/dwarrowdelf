using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public abstract class StaticSerialActionJob : DynamicSerialActionJob
	{
		ObservableCollection<IActionJob> m_subJobs;
		public ReadOnlyObservableCollection<IActionJob> SubJobs { get; private set; }

		protected StaticSerialActionJob(IJob parent, ActionPriority priority)
			: base(parent, priority)
		{
		}

		protected void SetSubJobs(IEnumerable<IActionJob> jobs)
		{
			m_subJobs = new ObservableCollection<IActionJob>(jobs);
			this.SubJobs = new ReadOnlyObservableCollection<IActionJob>(m_subJobs);
		}

		protected override IEnumerator<IActionJob> GetJobEnumerator()
		{
			return m_subJobs.GetEnumerator();
		}

		protected override void AbortOverride()
		{
			foreach (var job in m_subJobs)
				job.Abort();
		}

		protected override Progress CheckProgress()
		{
			if (this.SubJobs.All(j => j.Progress == Progress.Done))
				return Progress.Done;
			else
				return Progress.Ok;
		}
	}
}
