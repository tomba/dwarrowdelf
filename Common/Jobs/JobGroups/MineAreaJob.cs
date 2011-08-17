using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class MineAreaJob : JobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntCuboid m_area;

		IEnumerable<IntPoint3D> m_locs;

		class MineData
		{
			public IAssignment Assignment;
			public bool IsPossible;
		}

		Dictionary<IntPoint3D, MineData> m_map = new Dictionary<IntPoint3D, MineData>();

		public MineAreaJob(IEnvironment env, IntCuboid area, MineActionType mineActionType)
			: base(null)
		{
			m_environment = env;
			m_area = area;

			m_locs = area.Range().Where(p => env.GetTerrain(p).IsMinable || m_environment.GetHidden(p));
			foreach (var p in m_locs)
				m_map[p] = new MineData() { IsPossible = CheckTile(p) };
		}

		protected override IEnumerable<IJob> GetJobs(ILiving living)
		{
			var designations = m_map
				.Where(kvp => kvp.Value.IsPossible && kvp.Value.Assignment == null)
				.OrderBy(kvp => (kvp.Key - living.Location).Length);

			foreach (var kvp in designations)
			{
				var p = kvp.Key;
				var job = new AssignmentGroups.MoveMineAssignment(this, m_environment, p, MineActionType.Mine);
				AddSubJob(job);
				m_map[p].Assignment = job;
				yield return job;
			}
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			m_map.Remove(m_map.First(kvp => kvp.Value.Assignment == job).Key);

			foreach (var kvp in m_map.Where(kvp => kvp.Value.Assignment == null))
				kvp.Value.IsPossible = CheckTile(kvp.Key);

			if (m_map.Count == 0)
				SetStatus(Jobs.JobStatus.Done);
		}

		bool CheckTile(IntPoint3D p)
		{
			// trivial validity check to remove AStar process for totally blocked tiles
			
			if (m_environment.GetHidden(p))
				return false;

			DirectionSet dirs = DirectionSet.Planar | DirectionSet.Up;
			foreach (var d in dirs.ToDirections())
			{
				if (m_environment.GetHidden(p + d))
					continue;

				if (!m_environment.GetInterior(p + d).IsBlocker)
					return true;
			}

			return false;
		}

		public override string ToString()
		{
			return "MineAreaJob";
		}
	}
}
