using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef]
	sealed class ConstructionSite : IJobSource, IAreaElement, IJobObserver
	{
		public BuildingID BuildingID;
		public EnvironmentObject Environment { get; private set; }
		public IntRectZ Area { get; private set; }

		public string Description { get { return "Construction (" + Buildings.GetBuildingInfo(this.BuildingID).Name + ")"; } }
		public SymbolID SymbolID { get { return Client.SymbolID.Contraption; } }
		public GameColor EffectiveColor { get { return GameColor.Gray; } }

		public ConstructionSite(EnvironmentObject environment, BuildingID buildingID, IntRectZ area)
		{
			this.Environment = environment;
			this.BuildingID = buildingID;
			this.Area = area;

			this.Environment.World.JobManager.AddJobSource(this);
		}

		void ConstructionSize(SaveGameContext ctx)
		{
		}

		public void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);
		}

		ConstructBuildingJob m_job;

		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			if (m_job == null)
			{
				m_job = new ConstructBuildingJob(this, this.Environment, this.Area, this.BuildingID);
				GameData.Data.Jobs.Add(m_job);
			}

			return m_job.FindAssignment(living);
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			GameData.Data.Jobs.Remove(m_job);
			m_job = null;

			if (status == JobStatus.Done || status == JobStatus.Fail)
			{
				this.Environment.RemoveAreaElement(this);
				this.Destruct();
			}
		}
	}
}
