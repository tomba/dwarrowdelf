using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class MineAreaJob : StaticAssignmentGroup
	{
		readonly IEnvironment m_environment;
		readonly IntCuboid m_area;

		IEnumerable<IntPoint3D> m_locs;

		public MineAreaJob(IEnvironment env, ActionPriority priority, IntCuboid area, MineActionType mineActionType)
			: base(null, priority)
		{
			m_environment = env;
			m_area = area;

			m_locs = area.Range().Where(p => env.GetInterior(p).IsMineable);

			List<IAssignment> jobs = new List<IAssignment>();
			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, priority, env, p, mineActionType);
				jobs.Add(job);
			}

			SetAssignments(jobs);
		}

		protected override void OnStateChanged(JobState state)
		{
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialSameJob";
		}
	}
}
