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
	[SaveGameObjectByRef]
	class InstallFurnitureManager : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		EnvironmentObject m_environment;

		[SaveGameProperty]
		List<InstallJobData> m_jobDataList;

		public InstallFurnitureManager(EnvironmentObject ob)
		{
			m_environment = ob;
			m_environment.World.JobManager.AddJobSource(this);

			m_jobDataList = new List<InstallJobData>();
		}

		InstallFurnitureManager(SaveGameContext ctx)
		{
		}

		[OnSaveGamePostDeserialization]
		void OnDeserialized()
		{
			m_environment.World.JobManager.AddJobSource(this);
		}

		public void AddInstallJob(ItemObject item, IntPoint3D location)
		{
			var data = new InstallJobData()
			{
				Mode = InstallMode.Install,
				Item = item,
				Location = location,
			};

			item.ReservedBy = this;

			m_jobDataList.Add(data);
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
							job = new InstallFurnitureJob(this, data.Item, m_environment, data.Location);
							break;

						case InstallMode.Uninstall:
							job = new MoveInstallFurnitureAssignment(this, data.Item, InstallMode.Uninstall);
							break;

						default:
							throw new Exception();
					}

					data.Job = job;
					GameData.Data.Jobs.Add(job);
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

			GameData.Data.Jobs.Remove(job);

			Debug.Assert(data.Item.ReservedBy == this);
			data.Item.ReservedBy = null;
		}

		#endregion

		[Serializable]
		class InstallJobData
		{
			public InstallMode Mode;
			public ItemObject Item;
			public IntPoint3D Location;
			public IJob Job;
		}
	}
}
