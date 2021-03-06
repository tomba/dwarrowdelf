﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.JobGroups;
using Dwarrowdelf.Jobs;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	sealed class CleanAreaJob : JobGroup
	{
		EnvironmentObject m_environment;
		IntGrid2Z m_area;

		Dictionary<IntVector3, IJob> m_map;

		public CleanAreaJob(IJobObserver parent, EnvironmentObject env, IntGrid2Z area)
			: base(parent)
		{
			m_environment = env;
			m_area = area;

			m_map = new Dictionary<IntVector3, IJob>();

			foreach (var p in m_area.Range())
				m_map[p] = null;

			m_environment.World.TickStarted += World_TickStarting;
		}

		protected override void Cleanup()
		{
			m_environment.World.TickStarted -= World_TickStarting;
		}

		void World_TickStarting()
		{
			Check();
		}

		void Check()
		{
			foreach (var p in m_area.Range())
			{
				if (m_map[p] != null)
					return;

				if (m_environment.GetTileData(p).IsClear == false)
					return;
			}

			SetStatus(JobStatus.Done);
		}

		protected override IEnumerable<IJob> GetJobs(ILivingObject living)
		{
			foreach (var p in m_area.Range())
			{
				if (m_map[p] == null)
				{
					var td = m_environment.GetTileData(p);

					if (!td.IsClear)
					{
						IJob job;

						if (td.HasFellableTree)
						{
							job = new Dwarrowdelf.Jobs.AssignmentGroups.MoveFellTreeAssignment(this, m_environment, p);
						}
						else
						{
							throw new NotImplementedException();
						}

						AddSubJob(job);
						m_map[p] = job;
					}
				}

				if (m_map[p] != null)
					yield return m_map[p];
			}
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);
			m_map[m_map.First(kvp => kvp.Value == job).Key] = null;

			Check();
		}

		protected override void OnSubJobAborted(IJob job)
		{
			RemoveSubJob(job);
			m_map[m_map.First(kvp => kvp.Value == job).Key] = null;
		}

		public override string ToString()
		{
			return "CleanAreaJob";
		}
	}
}
