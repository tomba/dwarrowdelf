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
		None,
		Mine,
		FellTree,
		CreateStairs,
	}

	static class Designations
	{

		static Designations()
		{
		}
	}

	class Designation : IJobSource
	{
		public Environment Environment { get; private set; }

		Dictionary<IntPoint3D, DesignationData> m_map;
		bool m_checkStatus;

		class DesignationData
		{
			public DesignationType Type;
			public IAssignment Assignment;
			public bool IsPossible;
		}

		public Designation(Environment env)
		{
			this.Environment = env;

			m_map = new Dictionary<IntPoint3D, DesignationData>();

			this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
			this.Environment.World.TickStartEvent += OnTickStartEvent;
			this.Environment.World.JobManager.AddJobSource(this);
		}

		void OnEnvironmentMapTileTerrainChanged(IntPoint3D obj)
		{
			m_checkStatus = true;
		}

		public DesignationType ContainsPoint(IntPoint3D p)
		{
			DesignationData data;
			if (m_map.TryGetValue(p, out data))
				return data.Type;
			else
				return DesignationType.None;
		}

		void OnTickStartEvent()
		{
			if (m_checkStatus)
			{
				foreach (var kvp in m_map)
					CheckTile(kvp.Key);
				m_checkStatus = false;
			}

			foreach (var job in m_map.Select(kvp => kvp.Value.Assignment))
			{
				Debug.Assert(job.JobState != JobState.Done);

				if (job.JobState != JobState.Ok)
					job.Retry();
			}
		}

		public void AddArea(IntCuboid area, DesignationType type)
		{
			switch (type)
			{
				case DesignationType.Mine:
				case DesignationType.CreateStairs:

					MineActionType mat;

					if (type == DesignationType.Mine)
						mat = MineActionType.Mine;
					else
						mat = MineActionType.Stairs;

					var walls = area.Range().Where(p => !m_map.ContainsKey(p) && this.Environment.GetInterior(p).ID == InteriorID.NaturalWall);

					foreach (var p in walls)
					{
						var job = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, this.Environment, p, mat);
						AddJob(p, type, job);
					}

					break;

				case DesignationType.FellTree:
					var trees = area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);

					var jobs = trees.Select(p => (IJob)new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p));

					foreach (var p in trees)
					{
						var job = new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p);
						AddJob(p, type, job);
					}
					break;
			}

			// XXX
			GameData.Data.MainWindow.MapControl.InvalidateTiles();
		}

		bool IJobSource.HasWork
		{
			get { return m_map != null && m_map.Count > 0; }
		}

		IAssignment IJobSource.GetJob(ILiving living)
		{
			var jobs = m_map
				.Where(kvp => kvp.Value.IsPossible && !kvp.Value.Assignment.IsAssigned && kvp.Value.Assignment.JobState == JobState.Ok)
				.OrderBy(kvp => (kvp.Key - living.Location).Length)
				.Select(kvp => kvp.Value.Assignment);

			foreach (var assignment in jobs)
			{
				var jobState = assignment.Assign(living);

				switch (jobState)
				{
					case JobState.Ok:
						return assignment;

					case JobState.Done:
						throw new Exception();

					case JobState.Abort:
					case JobState.Fail:
						break;

					default:
						throw new Exception();
				}
			}

			return null;
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
					// Retry at next tick
					break;

				default:
					throw new Exception();
			}
		}

		protected void AddJob(IntPoint3D p, DesignationType type, IAssignment job)
		{
			Debug.Assert(job != null);
			Debug.Assert(!m_map.ContainsKey(p));

			m_map[p] = new DesignationData();
			m_map[p].Type = type;
			m_map[p].Assignment = job;
			GameData.Data.Jobs.Add(job);
			job.StateChanged += OnJobStateChanged;

			CheckTile(p);
		}

		void RemoveJob(IntPoint3D p)
		{
			var job = m_map[p].Assignment;

			m_map.Remove(p);

			GameData.Data.Jobs.Remove(job);
			job.StateChanged -= OnJobStateChanged;
			if (job.JobState == JobState.Ok)
				job.Abort();

			// XXX
			GameData.Data.MainWindow.MapControl.InvalidateTiles();
		}

		void RemoveJob(IJob job)
		{
			var kvp = m_map.First(e => e.Value.Assignment == job);
			RemoveJob(kvp.Key);
		}

		void CheckTile(IntPoint3D p)
		{
			var data = m_map[p];

			DirectionSet dirs;

			// trivial validity check to remove AStar process for totally blocked tiles

			switch (data.Type)
			{
				case DesignationType.Mine:
					dirs = DirectionSet.Planar | DirectionSet.Up;
					break;

				case DesignationType.CreateStairs:
					dirs = DirectionSet.Planar | DirectionSet.Up | DirectionSet.Down;
					break;

				case DesignationType.FellTree:
					dirs = DirectionSet.Planar;
					break;

				default:
					throw new Exception();
			}

			foreach (var d in dirs.ToDirections())
			{
				if (!this.Environment.GetInterior(p + d).Blocker)
				{
					data.IsPossible = true;
					return;
				}
			}

			data.IsPossible = false;
		}
	}
}
