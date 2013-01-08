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
		Channel,
	}

	[SaveGameObject]
	sealed class Designation : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }

		[SaveGameProperty]
		Dictionary<IntPoint3, DesignationData> m_map;
		[SaveGameProperty]
		bool m_checkStatus;

		[Serializable]
		sealed class DesignationData
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

			m_map = new Dictionary<IntPoint3, DesignationData>();

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
					this.Environment.World.Jobs.Add(job);
			}
		}

		void OnEnvironmentMapTileTerrainChanged(IntPoint3 obj)
		{
			m_checkStatus = true;
		}

		public DesignationType ContainsPoint(IntPoint3 p)
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

		public void AddArea(IntGrid3 area, DesignationType type)
		{
			int origCount = m_map.Count;

			IEnumerable<IntPoint3> locations;

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

				case DesignationType.Channel:
					locations = area.Range()
						.Where(p => this.Environment.Contains(p));
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

				GameData.Data.MainWindow.MapControl.InvalidateRenderViewTile(p);
			}

			if (origCount == 0 && m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting += OnTickStartEvent;
			}
		}

		public void RemoveArea(IntGrid3 area)
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
					case DesignationType.Channel:
						MineActionType mat;

						switch (dt.Type)
						{
							case DesignationType.Mine:
								mat = MineActionType.Mine;
								break;
							case DesignationType.CreateStairs:
								mat = MineActionType.Stairs;
								break;
							case DesignationType.Channel:
								mat = MineActionType.Channel;
								break;
							default:
								throw new Exception();
						}

						job = new Jobs.AssignmentGroups.MoveMineAssignment(this, this.Environment, p, mat);

						break;

					case DesignationType.FellTree:

						job = new Jobs.AssignmentGroups.MoveFellTreeAssignment(this, this.Environment, p);
						break;

					default:
						throw new Exception();
				}

				this.Environment.World.Jobs.Add(job);
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

		void RemoveDesignation(IntPoint3 p)
		{
			RemoveJob(p);

			m_map.Remove(p);

			if (m_map.Count == 0)
			{
				this.Environment.MapTileTerrainChanged -= OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarting -= OnTickStartEvent;
			}

			GameData.Data.MainWindow.MapControl.InvalidateRenderViewTile(p);
		}

		void RemoveJob(IntPoint3 p)
		{
			var job = m_map[p].Job;

			if (job != null)
			{
				this.Environment.World.Jobs.Remove(job);
				if (job.Status == JobStatus.Ok)
					job.Abort();

				m_map[p].Job = null;
			}
		}

		IntPoint3 FindLocationFromJob(IJob job)
		{
			return m_map.First(e => e.Value.Job == job).Key;
		}

		void CheckTile(IntPoint3 p)
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

				case DesignationType.Channel:
					dirs = DirectionSet.Planar;
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
