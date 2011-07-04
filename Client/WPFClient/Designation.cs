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

	[SaveGameObject]
	class Designation : IJobSource
	{
		[SaveGameProperty]
		public Environment Environment { get; private set; }

		[SaveGameProperty]
		Dictionary<IntPoint3D, DesignationData> m_map;
		[SaveGameProperty]
		bool m_checkStatus;

		[Serializable]
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

			this.Environment.World.JobManager.AddJobSource(this);
		}

		Designation(SaveGameContext ctx)
		{
		}

		[OnSaveGameDeserialized]
		void OnDeserialized()
		{
			this.Environment.World.JobManager.AddJobSource(this);

			if (m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting += OnTickStartEvent;
			}

			foreach (var kvp in m_map)
			{
				var job = kvp.Value.Assignment;
				if (job != null)
				{
					GameData.Data.Jobs.Add(job);
					job.StatusChanged += OnJobStatusChanged;
				}
			}
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
		}

		public void AddArea(IntCuboid area, DesignationType type)
		{
			int origCount = m_map.Count;

			IEnumerable<IntPoint3D> locations;

			switch (type)
			{
				case DesignationType.Mine:
				case DesignationType.CreateStairs:
					locations = area.Range().Where(p => !m_map.ContainsKey(p) &&
						(this.Environment.GetInterior(p).IsMineable || this.Environment.GetHidden(p)));
					break;

				case DesignationType.FellTree:
					locations = area.Range().Where(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);
					break;

				default:
					throw new Exception();
			}

			foreach (var p in locations)
			{
				m_map[p] = new DesignationData();
				m_map[p].Type = type;
				CheckTile(p);
			}

			if (origCount == 0 && m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting += OnTickStartEvent;
			}

			// XXX
			GameData.Data.MainWindow.MapControl.InvalidateTiles();
		}

		public void RemoveArea(IntCuboid area)
		{
			var removes = m_map.Where(kvp => area.Contains(kvp.Key)).ToArray();
			foreach (var kvp in removes)
				RemoveDesignation(kvp.Key);
		}

		bool IJobSource.HasWork
		{
			get { return m_map.Count > 0 && m_map.Any(dt => dt.Value.Assignment == null); }
		}

		IAssignment IJobSource.GetJob(ILiving living)
		{
			var designations = m_map
				.Where(kvp => kvp.Value.IsPossible && kvp.Value.Assignment == null)
				.OrderBy(kvp => (kvp.Key - living.Location).Length);

			foreach (var d in designations)
			{
				var p = d.Key;
				var dt = d.Value;

				IAssignment assignment;

				switch (dt.Type)
				{
					case DesignationType.Mine:
					case DesignationType.CreateStairs:
						MineActionType mat;

						if (dt.Type == DesignationType.Mine)
							mat = MineActionType.Mine;
						else
							mat = MineActionType.Stairs;

						assignment = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, this.Environment, p, mat);

						break;

					case DesignationType.FellTree:

						assignment = new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, this.Environment, p);
						break;

					default:
						throw new Exception();
				}

				var jobState = assignment.Assign(living);

				switch (jobState)
				{
					case JobStatus.Ok:
						GameData.Data.Jobs.Add(assignment);
						assignment.StatusChanged += OnJobStatusChanged;
						dt.Assignment = assignment;
						return assignment;

					case JobStatus.Done:
						throw new Exception();

					case JobStatus.Abort:
					case JobStatus.Fail:
						break;

					default:
						throw new Exception();
				}
			}

			return null;
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
					RemoveDesignation(FindLocationFromJob(job));
					break;

				case JobStatus.Abort:
				case JobStatus.Fail:
					RemoveJob(FindLocationFromJob(job));
					break;

				default:
					throw new Exception();
			}
		}

		void RemoveDesignation(IntPoint3D p)
		{
			RemoveJob(p);

			m_map.Remove(p);

			if (m_map.Count == 0)
			{
				this.Environment.MapTileTerrainChanged -= OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting -= OnTickStartEvent;
			}

			// XXX
			GameData.Data.MainWindow.MapControl.InvalidateTiles();
		}

		void RemoveJob(IntPoint3D p)
		{
			var job = m_map[p].Assignment;

			if (job != null)
			{
				GameData.Data.Jobs.Remove(job);
				job.StatusChanged -= OnJobStatusChanged;
				if (job.JobStatus == JobStatus.Ok)
					job.Abort();

				m_map[p].Assignment = null;
			}
		}

		IntPoint3D FindLocationFromJob(IJob job)
		{
			return m_map.First(e => e.Value.Assignment == job).Key;
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
