using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.JobGroups;
using System.Diagnostics;
using Dwarrowdelf.Jobs.AssignmentGroups;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public class InstallItemManager : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		EnvironmentObject m_environment;

		[SaveGameProperty]
		List<InstallJobData> m_jobDataList;

		public InstallItemManager(EnvironmentObject env)
		{
			m_environment = env;

			m_jobDataList = new List<InstallJobData>();
		}

		InstallItemManager(SaveGameContext ctx)
		{
		}

		public void Register()
		{
			m_environment.World.JobManager.AddJobSource(this);
		}

		public void Unregister()
		{
			m_environment.World.JobManager.RemoveJobSource(this);
		}

		public void AddInstallJob(ItemObject item, IntPoint3 location)
		{
			var data = new InstallJobData()
			{
				Mode = InstallMode.Install,
				Item = item,
				Location = location,
			};

			item.ReservedBy = this;

			m_jobDataList.Add(data);

			m_environment.OnTileExtraChanged(location);
		}

		public void AddUninstallJob(ItemObject item)
		{
			var data = new InstallJobData()
			{
				Mode = InstallMode.Uninstall,
				Item = item,
			};

			item.ReservedBy = this;

			m_jobDataList.Add(data);
		}

		public ItemObject ContainsPoint(IntPoint3 p)
		{
			var data = m_jobDataList.Where(d => d.Mode == InstallMode.Install && d.Location == p).FirstOrDefault();
			if (data == null)
				return null;
			return data.Item;
		}

		#region IJobSource Members

		public IAssignment FindAssignment(ILivingObject living)
		{
			if (m_jobDataList.Count == 0)
				return null;

			foreach (var data in m_jobDataList)
			{
				if (data.Job == null)
				{
					IJob job;

					switch (data.Mode)
					{
						case InstallMode.Install:
							job = new InstallItemJob(this, data.Item, m_environment, data.Location);
							break;

						case InstallMode.Uninstall:
							job = new MoveInstallItemAssignment(this, data.Item, InstallMode.Uninstall);
							break;

						default:
							throw new Exception();
					}

					data.Job = job;
					m_environment.World.Jobs.Add(job);
				}

				IAssignment assignment;

				if (data.Job is IJobGroup)
				{
					assignment = ((IJobGroup)data.Job).FindAssignment(living);
				}
				else
				{
					assignment = (IAssignment)data.Job;
					if (assignment.IsAssigned)
						assignment = null;
				}

				if (assignment != null)
					return assignment;
			}

			return null;
		}

		#endregion

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var data = m_jobDataList.Single(d => d.Job == job);

			m_jobDataList.Remove(data);

			m_environment.World.Jobs.Remove(job);

			Debug.Assert(data.Item.ReservedBy == this);
			data.Item.ReservedBy = null;
		}

		#endregion

		[Serializable]
		class InstallJobData
		{
			public InstallMode Mode;
			public ItemObject Item;
			public IntPoint3 Location;
			public IJob Job;
		}
	}
}
