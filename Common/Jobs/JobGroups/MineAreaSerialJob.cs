﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class MineAreaSerialJob : SerialJobGroup
	{
		readonly IEnvironment m_environment;
		readonly IntRect m_rect;

		IEnumerable<IntPoint> m_locs;

		public MineAreaSerialJob(IEnvironment env, ActionPriority priority, IntRect rect, int z)
			: base(null, priority)
		{
			m_environment = env;
			m_rect = rect;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.Wall);

			foreach (var p in m_locs)
			{
				var job = new AssignmentGroups.MoveMineJob(this, priority, env, new IntPoint3D(p, z));
				AddSubJob(job);
			}
		}

		protected override void Cleanup()
		{
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialJob";
		}
	}

}