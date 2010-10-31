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
		FellTree,
		CreateStairs,
	}

	abstract class Designation : IDrawableArea, IJobSource
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
		}

		public void Start()
		{
			m_job = CreateJob();

			if (m_job == null)
			{
				if (this.DesignationDone != null)
					this.DesignationDone(this);
				return;
			}

			m_job.StateChanged += OnJobStateChanged;

			this.Environment.World.JobManager.AddJobSource(this);
			GameData.Data.Jobs.Add(m_job);
		}

		protected abstract IJob CreateJob();

		bool IJobSource.HasWork
		{
			get
			{
				return m_job != null;
			}
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			if (m_job != null && m_job.JobState == JobState.Ok)
				yield return m_job;
		}

		void IJobSource.JobTaken(ILiving living, IJob job)
		{
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			Debug.Assert(job == m_job);

			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
					Abort();
					break;

				case JobState.Abort:
					job.Retry();
					break;

				case JobState.Fail:
					Abort();
					break;

				default:
					throw new Exception();
			}
		}

		public void Abort()
		{
			if (m_job == null)
				return;

			GameData.Data.Jobs.Remove(m_job);

			m_job.StateChanged -= OnJobStateChanged;
			m_job.Abort();
			m_job = null;

			if (this.DesignationDone != null)
				this.DesignationDone(this);

			this.Environment.World.JobManager.RemoveJobSource(this);
		}

		public event Action<Designation> DesignationDone;
	}


	class MineDesignation : Designation
	{
		MineActionType m_mineActionType;

		public MineDesignation(Environment env, IntCuboid area, MineActionType mineActionType)
			: base(env, DesignationType.Mine, area)
		{
			m_mineActionType = mineActionType;
		}

		protected override IJob CreateJob()
		{
			var anyWalls = this.Area.Range().Any(p => this.Environment.GetInterior(p).ID == InteriorID.NaturalWall);

			if (!anyWalls)
				return null;

			return new Jobs.JobGroups.MineAreaParallelJob(this.Environment, ActionPriority.Normal, this.Area, m_mineActionType);
		}
	}

	class FellTreeDesignation : Designation
	{
		public FellTreeDesignation(Environment env, IntCuboid area)
			: base(env, DesignationType.FellTree, area)
		{
		}

		protected override IJob CreateJob()
		{
			var anyTrees = this.Area.Range().Any(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);

			if (!anyTrees)
				return null;

			return new Jobs.JobGroups.FellTreeParallelJob(this.Environment, ActionPriority.Normal, this.Area);
		}
	}

	class DesignationManager
	{
		ObservableCollection<Designation> s_designations;
		public ReadOnlyObservableCollection<Designation> Designations { get; private set; }

		public DesignationManager()
		{
			s_designations = new ObservableCollection<Designation>();
			Designations = new ReadOnlyObservableCollection<Designation>(s_designations);
		}

		public void AddDesignation(Designation designation)
		{
			s_designations.Add(designation);
			designation.Start();
		}

		public void RemoveDesignation(Designation designation)
		{
			designation.Abort();
			s_designations.Remove(designation);
		}
	}
}
