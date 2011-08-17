using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.JobGroups;
using Dwarrowdelf.Jobs;
using System.Diagnostics;
using Dwarrowdelf.Jobs.AssignmentGroups;

namespace Dwarrowdelf.Client
{
	class ConstructBuildingJob : JobGroup
	{
		Environment m_environment;
		IntRectZ m_area;
		BuildingID m_buildingID;
		int m_state;

		public ConstructBuildingJob(IJob parent, Environment env, IntRectZ area, BuildingID buildingID)
			: base(parent)
		{
			m_environment = env;
			m_area = area;
			m_buildingID = buildingID;

			m_state = 0;
			AddSubJob(new CleanAreaJob(this, m_environment, m_area));
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;

				AddSubJob(new MoveConstructBuildingAssignment(this, m_environment, m_area, m_buildingID));
			}
			else if (m_state == 1)
			{
				SetStatus(Jobs.JobStatus.Done);
			}
			else
			{
				throw new Exception();
			}
		}

		protected override void OnSubJobAborted(IJob job)
		{
			if (m_state == 1)
			{
				RemoveSubJob(job);
				AddSubJob(new MoveConstructBuildingAssignment(this, m_environment, m_area, m_buildingID));
				return;
			}

			base.OnSubJobAborted(job);
		}

		public override string ToString()
		{
			return "ConstructBuildingJob";
		}
	}
}
