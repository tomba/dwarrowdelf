using System;
using System.Collections.Generic;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	enum DesignationType
	{
		None,
		Mine,
		FellTree,
		CreateStairs,
	}

	[SaveGameObjectByRef]
	class Designation : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }

		[SaveGameProperty]
		Dictionary<IntPoint3D, DesignationData> m_map;
		[SaveGameProperty]
		bool m_checkStatus;

		[Serializable]
		class DesignationData
		{
			public DesignationData(DesignationType type)
			{
				this.Type = type;
			}

			public DesignationType Type;
			public IJob Job;
			public bool IsPossible;
		}

		public Designation(EnvironmentObject env)
		{
			this.Environment = env;

			m_map = new Dictionary<IntPoint3D, DesignationData>();

			this.Environment.World.JobManager.AddJobSource(this);
		}

		Designation(SaveGameContext ctx)
		{
		}

		[OnSaveGamePostDeserialization]
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
				var job = kvp.Value.Job;
				if (job != null)
					GameData.Data.Jobs.Add(job);
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
					locations = area.Range()
						.Where(p => this.Environment.Contains(p))
						.Where(p => this.Environment.GetTerrain(p).IsMinable || this.Environment.GetHidden(p));
					break;

				case DesignationType.CreateStairs:
					locations = area.Range()
						.Where(p => this.Environment.Contains(p))
						.Where(p => (this.Environment.GetTerrain(p).IsMinable && this.Environment.GetTerrainID(p) == TerrainID.NaturalWall) || this.Environment.GetHidden(p));
					break;

				case DesignationType.FellTree:
					locations = area.Range()
						.Where(p => this.Environment.Contains(p))
						.Where(p => this.Environment.GetInterior(p).ID == InteriorID.Tree);
					break;

				default:
					throw new Exception();
			}

			foreach (var p in locations)
			{
				DesignationData oldData;

				if (m_map.TryGetValue(p, out oldData))
				{
					if (oldData.Type == type)
						continue;

					RemoveJob(p);
				}

				m_map[p] = new DesignationData(type);
				CheckTile(p);
			}

			if (origCount == 0 && m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting += OnTickStartEvent;
			}

			// XXX
			GameData.Data.MainWindow.MapControl.InvalidateTileData();
		}

		public void RemoveArea(IntCuboid area)
		{
			var removes = m_map.Where(kvp => area.Contains(kvp.Key)).ToArray();
			foreach (var kvp in removes)
				RemoveDesignation(kvp.Key);
		}

		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			var designations = m_map
				.Where(kvp => kvp.Value.IsPossible && kvp.Value.Job == null)
				.OrderBy(kvp => (kvp.Key - living.Location).Length);

			foreach (var d in designations)
			{
				var p = d.Key;
				var dt = d.Value;

				IAssignment job;

				switch (dt.Type)
				{
					case DesignationType.Mine:
					case DesignationType.CreateStairs:
						MineActionType mat;

						if (dt.Type == DesignationType.Mine)
							mat = MineActionType.Mine;
						else
							mat = MineActionType.Stairs;

						job = new Jobs.AssignmentGroups.MoveMineAssignment(this, this.Environment, p, mat);

						break;

					case DesignationType.FellTree:

						job = new Jobs.AssignmentGroups.MoveFellTreeAssignment(this, this.Environment, p);
						break;

					default:
						throw new Exception();
				}

				GameData.Data.Jobs.Add(job);
				dt.Job = job;

				return job;
			}

			return null;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
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
			GameData.Data.MainWindow.MapControl.InvalidateTileData();
		}

		void RemoveJob(IntPoint3D p)
		{
			var job = m_map[p].Job;

			if (job != null)
			{
				GameData.Data.Jobs.Remove(job);
				if (job.Status == JobStatus.Ok)
					job.Abort();

				m_map[p].Job = null;
			}
		}

		IntPoint3D FindLocationFromJob(IJob job)
		{
			return m_map.First(e => e.Value.Job == job).Key;
		}

		void CheckTile(IntPoint3D p)
		{
			var data = m_map[p];

			DirectionSet dirs;

			// trivial validity check to remove AStar process for totally blocked tiles

			switch (data.Type)
			{
				case DesignationType.Mine:
					dirs = DirectionSet.Planar;
					// If the tile below has stairs, tile tile can be mined from below
					if (EnvironmentHelpers.CanMoveFrom(this.Environment, p + Direction.Down, Direction.Up))
						dirs |= DirectionSet.Down;
					break;

				case DesignationType.CreateStairs:
					dirs = DirectionSet.Planar | DirectionSet.Up;
					if (EnvironmentHelpers.CanMoveFrom(this.Environment, p + Direction.Down, Direction.Up))
						dirs |= DirectionSet.Down;
					break;

				case DesignationType.FellTree:
					dirs = DirectionSet.Planar;
					break;

				default:
					throw new Exception();
			}

			foreach (var d in dirs.ToDirections())
			{
				if (EnvironmentHelpers.CanEnter(this.Environment, p + d))
				{
					data.IsPossible = true;
					return;
				}
			}

			data.IsPossible = false;
		}
	}
}
