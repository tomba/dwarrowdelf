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

		class PositionInfo
		{
			public bool Failed;
			public IAssignment Job;
		}

		Dictionary<IntPoint3D, PositionInfo> m_map;

		public Designation(Environment env, DesignationType type, IntCuboid area)
		{
			this.Environment = env;
			this.Type = type;
			this.Area = area;

			var rect = new Rectangle();
			rect.Fill = Brushes.DimGray;
			rect.Opacity = 0.5;
			rect.Stroke = Brushes.DarkGray;
			rect.StrokeThickness = 0.1;
			rect.Width = area.Width;
			rect.Height = area.Height;
			m_element = rect;

			this.Environment.World.TickStartEvent += OnTickStartEvent;

			this.Environment.World.JobManager.AddJobSource(this);
		}

		void OnTickStartEvent()
		{
			foreach (var kvp in m_map)
				kvp.Value.Failed = false;

			Check();
		}

		public void Start()
		{
			Debug.Assert(m_map == null);

			var positions = InitializeOverride();
			m_map = positions.ToDictionary(p => p, p => new PositionInfo());

			CheckOverride(positions);
		}

		protected abstract IntPoint3D[] InitializeOverride();

		void Check()
		{
			var positions = m_map.Keys.ToArray();

			CheckOverride(positions);

			if (m_map.Count == 0)
			{
				if (this.DesignationDone != null)
					this.DesignationDone(this);

				Abort();
			}
		}

		protected abstract void CheckOverride(IntPoint3D[] positions);

		bool IJobSource.HasWork
		{
			get
			{
				return m_map != null && m_map.Count > 0;
			}
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			foreach (var kvp in m_map)
			{
				var p = kvp.Key;
				var info = kvp.Value;

				if (info.Failed)
					continue;

				var job = info.Job;

				if (job != null)
					continue;

				job = GetJob(living, p);

				if (job == null)
				{
					info.Failed = true;
					continue;
				}

				Add(p, job);
				yield return job;
			}
		}

		protected abstract IAssignment GetJob(ILiving living, IntPoint3D pos);

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
					break;

				case JobState.Abort:
				case JobState.Fail:
					RestartJob(job);
					break;

				default:
					throw new Exception();
			}
		}

		public void Abort()
		{
			if (m_map == null)
				return;

			var map = m_map;
			m_map = null;

			this.Environment.World.TickStartEvent -= OnTickStartEvent;
			this.Environment.World.JobManager.RemoveJobSource(this);

			foreach (var kvp in map)
				Remove(kvp.Key);

			if (this.DesignationDone != null)
				this.DesignationDone(this);
		}

		protected IAssignment Get(IntPoint3D p)
		{
			return m_map[p].Job;
		}

		protected void Add(IntPoint3D p, IAssignment job)
		{
			Debug.Assert(job != null);
			Debug.Assert(m_map[p].Job == null);

			m_map[p].Job = job;
			GameData.Data.Jobs.Add(job);
			job.StateChanged += OnJobStateChanged;
		}

		protected void Remove(IntPoint3D p)
		{
			var job = m_map[p].Job;

			m_map.Remove(p);

			GameData.Data.Jobs.Remove(job);
			job.StateChanged -= OnJobStateChanged;
			if (job.JobState == JobState.Ok)
				job.Abort();
		}

		void RemoveJob(IJob job)
		{
			var kvp = m_map.First(e => e.Value.Job == job);
			Remove(kvp.Key);
		}

		void RestartJob(IJob job)
		{
			var kvp = m_map.First(e => e.Value.Job == job);
			m_map[kvp.Key].Job = null;

			GameData.Data.Jobs.Remove(job);
			job.StateChanged -= OnJobStateChanged;
			if (job.JobState == JobState.Ok)
				job.Abort();
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

		protected override IntPoint3D[] InitializeOverride()
		{
			var walls = this.Area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.NaturalWall);
			return walls.ToArray();
		}

		protected override void CheckOverride(IntPoint3D[] positions)
		{
			return;
			foreach (var p in positions)
			{
				if (this.Environment.GetInterior(p).ID != InteriorID.NaturalWall)
				{
					Remove(p);
					continue;
				}

				var job = Get(p);

				if (job == null)
				{
					var pos = GetPossiblePositioning(p);

					if (pos == Positioning.Exact)
						continue;

					job = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, this.Environment, p, m_mineActionType, pos);
					Add(p, job);
				}
				else
				{
					Debug.Assert(job.JobState == JobState.Ok);
				}
			}
		}

		protected override IAssignment GetJob(ILiving living, IntPoint3D p)
		{
			var pos = GetPossiblePositioning(p);
			IntPoint3D finalPos;

			var path = AStar.AStar.Find(this.Environment, living.Location, p, pos, out finalPos);

			if (path == null)
				return null;

			var job = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, this.Environment, p, m_mineActionType, finalPos);

			return job;
		}

		Positioning GetPossiblePositioning(IntPoint3D p)
		{
			var env = this.Environment;


			// TODO 

			return Positioning.AdjacentPlanar;
		}
	}
	/*
	class FellTreeDesignation : Designation
	{
		public FellTreeDesignation(Environment env, IntCuboid area)
			: base(env, DesignationType.FellTree, area)
		{
		}

		protected override List<IJob> CreateJobs()
		{
			var trees = this.Area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);

			var jobs = trees.Select(p => (IJob)new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p));

			return jobs.ToList();
		}
	}
	*/
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
