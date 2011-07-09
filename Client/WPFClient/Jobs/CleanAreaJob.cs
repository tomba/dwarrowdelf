using System;
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
	class CleanAreaJob : JobGroup
	{
		Environment m_environment;
		IntRectZ m_area;

		Dictionary<IntPoint3D, IJob> m_map;

		public CleanAreaJob(IJob parent, ActionPriority priority, Environment env, IntRectZ area)
			: base(parent, priority)
		{
			m_environment = env;
			m_area = area;

			m_map = new Dictionary<IntPoint3D, IJob>();

			foreach (var p in m_area.Range())
				m_map[p] = null;

			Check();
		}

		void Check()
		{
			foreach (var p in m_area.Range())
			{
				if (m_map[p] != null)
					return;

				if (m_environment.GetInteriorID(p) != InteriorID.Empty)
					return;

				if (m_environment.GetContents(p).OfType<ItemObject>().Count() > 0)
					return;
			}

			SetStatus(Jobs.JobStatus.Done);
		}

		protected override IEnumerable<IJob> GetJobs(ILiving living)
		{
			foreach (var p in m_area.Range())
			{
				if (m_map[p] == null)
				{
					IJob job = null;

					var iid = m_environment.GetInteriorID(p);

					if (iid != InteriorID.Empty)
					{
						if (iid == InteriorID.Tree)
						{
							job = new Dwarrowdelf.Jobs.AssignmentGroups.MoveFellTreeJob(this, ActionPriority.Normal, m_environment, p);
						}
						else
						{
							throw new NotImplementedException();
						}
					}
					else
					{
						var ob = m_environment.GetContents(p).OfType<ItemObject>().FirstOrDefault();

						if (ob != null)
							job = new Dwarrowdelf.Jobs.AssignmentGroups.FetchItem(this, this.Priority, m_environment, m_area.Center - new IntVector3D(-5, 0, 0), ob);
					}

					if (job != null)
					{
						AddSubJob(job);
						m_map[p] = job;
					}
				}

				if (m_map[p] != null)
					yield return m_map[p];
			}
		}

		protected override void OnSubJobStatusChanged(IJob job, JobStatus status)
		{
			if (status == Jobs.JobStatus.Ok)
				throw new Exception();

			if (status == Jobs.JobStatus.Abort || status == Jobs.JobStatus.Fail)
				throw new Exception();

			RemoveSubJob(job);
			m_map[m_map.First(kvp => kvp.Value == job).Key] = null;

			Check();
		}

		public override string ToString()
		{
			return "CleanAreaJob";
		}
	}
}
