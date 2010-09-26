using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	enum DesignationType
	{
		Mine,
	}

	class Designation : IDrawableArea
	{
		public DesignationType Type { get; private set; }
		public IntCuboid Area { get; private set; }
		public Environment Environment { get; private set; }

		public Brush Fill { get { return Brushes.DimGray; } }
		public double Opacity { get { return 0.5; } }

		IJob m_job;

		public Designation(Environment env, DesignationType type, IntCuboid area)
		{
			this.Environment = env;
			this.Type = type;
			this.Area = area;

			s_designations.Add(this);

			m_job = new Jobs.JobGroups.MineAreaParallelJob(this.Environment, ActionPriority.Normal, this.Area);
			m_job.PropertyChanged += OnJobPropertyChanged;
			this.Environment.World.JobManager.Add(m_job);
		}

		void OnJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "Progress")
				return;

			var job = (IJob)sender;
			Debug.Assert(job == m_job);

			if (job.Progress == Progress.Done)
				Remove();
		}

		public void Remove()
		{
			s_designations.Remove(this);

			if (m_job == null)
				return;

			m_job.Abort();
			this.Environment.World.JobManager.Remove(m_job);
		}

		static ObservableCollection<Designation> s_designations;
		public static ReadOnlyObservableCollection<Designation> Designations { get; private set; }

		static Designation()
		{
			s_designations = new ObservableCollection<Designation>();
			Designations = new ReadOnlyObservableCollection<Designation>(s_designations);
		}
	}
}
