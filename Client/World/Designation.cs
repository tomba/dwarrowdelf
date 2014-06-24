using System;
using System.Collections.Generic;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	public enum DesignationType
	{
		None,
		Mine,
		FellTree,
		CreateStairs,
		Channel,
	}

	[SaveGameObject]
	public sealed class Designation : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }

		[SaveGameProperty]
		Dictionary<IntVector3, DesignationData> m_map;

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
			public bool ReachableSimple;
			public int NextReacahbleCheck;
		}

		public Designation(EnvironmentObject env)
		{
			this.Environment = env;

			m_map = new Dictionary<IntVector3, DesignationData>();
		}

		Designation(SaveGameContext ctx)
		{
		}

		public void Register()
		{
			this.Environment.World.JobManager.AddJobSource(this);

			if (m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarted += OnTickStartEvent;
			}

			foreach (var kvp in m_map)
			{
				var job = kvp.Value.Job;
				if (job != null)
					this.Environment.World.Jobs.Add(job);
			}
		}

		public void Unregister()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			foreach (var kvp in m_map)
				RemoveDesignation(kvp.Key);
		}

		void OnEnvironmentMapTileTerrainChanged(IntVector3 obj)
		{
			m_checkStatus = true;
		}

		public DesignationType ContainsPoint(IntVector3 p)
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
				var rm = new List<IntVector3>();

				foreach (var kvp in m_map)
				{
					var p = kvp.Key;
					var data = kvp.Value;

					if (GetTileValid(p, data.Type) == false)
						rm.Add(p);

					bool reachable = GetTileReachableSimple(p, data.Type);

					if (data.ReachableSimple == false && reachable)
						data.NextReacahbleCheck = 0;

					data.ReachableSimple = reachable;
				}

				foreach (var p in rm)
					RemoveDesignation(p);

				m_checkStatus = false;
			}
		}

		public void AddArea(IntGrid3 area, DesignationType type)
		{
			int origCount = m_map.Count;

			var locations = area.Range().Where(this.Environment.Contains);

			foreach (var p in locations)
			{
				if (GetTileValid(p, type) == false)
					continue;

				DesignationData oldData;

				if (m_map.TryGetValue(p, out oldData))
				{
					if (oldData.Type == type)
						continue;

					RemoveJob(p);
				}

				var data = new DesignationData(type);
				data.ReachableSimple = GetTileReachableSimple(p, type);
				m_map[p] = data;

				this.Environment.OnTileExtraChanged(p);
			}

			if (origCount == 0 && m_map.Count > 0)
			{
				this.Environment.MapTileTerrainChanged += OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarted += OnTickStartEvent;
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
			var tick = this.Environment.World.TickNumber;

			var designations = m_map
				.Where(kvp => kvp.Value.Job == null && kvp.Value.ReachableSimple && kvp.Value.NextReacahbleCheck <= tick)
				.OrderBy(kvp => (kvp.Key - living.Location).Length);

			foreach (var d in designations)
			{
				var p = d.Key;
				var dt = d.Value;

				var ds = GetDesignationPositioning(p, dt.Type);

				// XXX we should pass the found path to the job, to avoid re-pathing
				bool canreach = AStar.CanReach(this.Environment, living.Location, p, ds);
				if (!canreach)
				{
					dt.NextReacahbleCheck = tick + 10;
					continue;
				}

				if (dt.Type == DesignationType.Channel)
				{
					if (this.Environment.HasContents(p))
					{
						dt.NextReacahbleCheck = tick + 10;
						continue;
					}
				}

				IAssignment job;

				switch (dt.Type)
				{
					case DesignationType.Mine:
					case DesignationType.CreateStairs:
					case DesignationType.Channel:
						MineActionType mat = DesignationTypeToMineActionType(dt.Type);

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
			var p = FindLocationFromJob(job);

			switch (status)
			{
				case JobStatus.Ok:
					break;

				case JobStatus.Done:
					RemoveDesignation(p);
					break;

				case JobStatus.Abort:
					RemoveJob(p);
					break;

				case JobStatus.Fail:
					RemoveDesignation(p);
					break;

				default:
					throw new Exception();
			}
		}

		void RemoveDesignation(IntVector3 p)
		{
			RemoveJob(p);

			m_map.Remove(p);

			if (m_map.Count == 0)
			{
				this.Environment.MapTileTerrainChanged -= OnEnvironmentMapTileTerrainChanged;
				this.Environment.World.TickStarted -= OnTickStartEvent;
			}

			this.Environment.OnTileExtraChanged(p);
		}

		void RemoveJob(IntVector3 p)
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

		IntVector3 FindLocationFromJob(IJob job)
		{
			return m_map.First(e => e.Value.Job == job).Key;
		}

		bool GetTileValid(IntVector3 p, DesignationType type)
		{
			var td = this.Environment.GetTileData(p);

			switch (type)
			{
				case DesignationType.Mine:
					return this.Environment.GetHidden(p) || td.IsMinable;

				case DesignationType.CreateStairs:
					return this.Environment.GetHidden(p) || (td.IsMinable && td.InteriorID == InteriorID.NaturalWall);

				case DesignationType.Channel:
					return this.Environment.GetHidden(p) == false && td.IsClearFloor &&
						(this.Environment.GetHidden(p.Down) || this.Environment.GetTileData(p.Down).IsMinable);

				case DesignationType.FellTree:
					return td.InteriorID.IsFellableTree();

				default:
					throw new Exception();
			}
		}

		MineActionType DesignationTypeToMineActionType(DesignationType dtype)
		{
			switch (dtype)
			{
				case DesignationType.Mine:
					return MineActionType.Mine;
				case DesignationType.CreateStairs:
					return MineActionType.Stairs;
				case DesignationType.Channel:
					return MineActionType.Channel;
				default:
					throw new Exception();
			}
		}

		/// <summary>
		/// trivial validity check to remove AStar process for totally blocked tiles
		/// </summary>
		bool GetTileReachableSimple(IntVector3 p, DesignationType type)
		{
			DirectionSet dirs = GetDesignationPositioning(p, type);

			return dirs.ToSurroundingPoints(p).Any(this.Environment.CanEnter);
		}

		DirectionSet GetDesignationPositioning(IntVector3 p, DesignationType type)
		{
			switch (type)
			{
				case DesignationType.Mine:
				case DesignationType.CreateStairs:
				case DesignationType.Channel:
					return this.Environment.GetPossibleMiningPositioning(p, DesignationTypeToMineActionType(type));

				case DesignationType.FellTree:
					return DirectionSet.Planar;

				default:
					throw new Exception();
			}
		}
	}
}
