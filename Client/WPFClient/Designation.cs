using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Diagnostics;
using System.Windows;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client
{
	enum DesignationType
	{
		Mine,
		FellTree,
		CreateStairs,
	}

	abstract class Designation : IDrawableElement, IJobSource
	{
		public DesignationType Type { get; private set; }
		public IntCuboid Area { get; private set; }
		public Environment Environment { get; private set; }

		FrameworkElement m_element;
		public FrameworkElement Element { get { return m_element; } }

		Dictionary<IntPoint3D, IAssignment> m_map;

		public Designation(Environment env, IntCuboid area, DesignationType designationType)
		{
			this.Environment = env;
			this.Type = designationType;
			this.Area = area;

			var rect = new Rectangle();
			rect.Fill = Brushes.DimGray;
			rect.Opacity = 0.5;
			rect.Stroke = Brushes.DarkGray;
			rect.StrokeThickness = 0.1;
			rect.Width = area.Width;
			rect.Height = area.Height;
			m_element = rect;
		}

		void OnTickStartEvent()
		{
			foreach (var kvp in m_map)
			{
				var job = kvp.Value;

				Debug.Assert(job.JobState != JobState.Done);

				if (job.JobState != JobState.Ok)
					job.Retry();
			}
		}

		public void Start()
		{
			m_map = new Dictionary<IntPoint3D, IAssignment>();

			CreateJobs();

			if (m_map.Count == 0)
			{
				Cleanup();
				return;
			}

			this.Environment.World.TickStartEvent += OnTickStartEvent;
			this.Environment.World.JobManager.AddJobSource(this);
		}

		public void Abort()
		{
			Cleanup();
		}

		void Cleanup()
		{
			if (m_map == null)
				return;

			this.Environment.World.TickStartEvent -= OnTickStartEvent;
			this.Environment.World.JobManager.RemoveJobSource(this);

			var locs = m_map.Keys.ToArray();

			foreach (var p in locs)
				RemoveJob(p);

			m_map = null;

			if (this.DesignationDone != null)
				this.DesignationDone(this);
		}

		protected abstract void CreateJobs();

		bool IJobSource.HasWork
		{
			get { return m_map != null && m_map.Count > 0; }
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			return m_map.Select(kvp => kvp.Value).Where(j => !j.IsAssigned && j.JobState == JobState.Ok);
		}

		void IJobSource.JobTaken(ILiving living, IJob job)
		{
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			switch (state)
			{
				case JobState.Ok:
					break;

				case JobState.Done:
					RemoveJob(job);

					if (m_map.Count == 0)
						Cleanup();

					break;

				case JobState.Abort:
				case JobState.Fail:
					// Retry at next tick
					break;

				default:
					throw new Exception();
			}
		}


		protected void AddJob(IntPoint3D p, IAssignment job)
		{
			Debug.Assert(job != null);
			Debug.Assert(!m_map.ContainsKey(p));

			m_map[p] = job;
			GameData.Data.Jobs.Add(job);
			job.StateChanged += OnJobStateChanged;
		}

		void RemoveJob(IntPoint3D p)
		{
			var job = m_map[p];

			m_map.Remove(p);

			GameData.Data.Jobs.Remove(job);
			job.StateChanged -= OnJobStateChanged;
			if (job.JobState == JobState.Ok)
				job.Abort();
		}

		void RemoveJob(IJob job)
		{
			var kvp = m_map.First(e => e.Value == job);
			RemoveJob(kvp.Key);
		}

		public event Action<Designation> DesignationDone;
	}

	class MineDesignation : Designation
	{
		MineActionType m_mineActionType;

		public MineDesignation(Environment env, IntCuboid area, MineActionType mineActionType)
			: base(env, area, DesignationType.Mine)
		{
			m_mineActionType = mineActionType;
		}

		protected override void CreateJobs()
		{
			var walls = this.Area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.NaturalWall);

			foreach (var p in walls)
			{
				var job = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, this.Environment, p, m_mineActionType);
				AddJob(p, job);
			}
		}
	}

	class FellTreeDesignation : Designation
	{
		public FellTreeDesignation(Environment env, IntCuboid area)
			: base(env, area, DesignationType.FellTree)
		{
		}

		protected override void CreateJobs()
		{
			var trees = this.Area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);

			var jobs = trees.Select(p => (IJob)new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p));

			foreach (var p in trees)
			{
				var job = new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p);
				AddJob(p, job);
			}
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
