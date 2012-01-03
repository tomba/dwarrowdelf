using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.JobGroups;

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

		public void AddJob(ItemObject item, IntPoint3D location)
		{
			var data = new InstallJobData()
			{
				Item = item,
				Location = location,
			};

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
					var job = new InstallFurnitureJob(this, data.Item, m_environment, data.Location);
					data.Job = job;
					GameData.Data.Jobs.Add(job);
				}

				var assignment = data.Job.FindAssignment(living);

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
		}

		#endregion

		[Serializable]
		class InstallJobData
		{
			public ItemObject Item;
			public IntPoint3D Location;
			public InstallFurnitureJob Job;
		}
	}
}
