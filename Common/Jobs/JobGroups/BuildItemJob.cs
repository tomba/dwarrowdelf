using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class BuildItemJob : JobGroup
	{
		IBuildingObject m_workplace;
		IItemObject[] m_sourceObjects;
		ItemID m_dstItemID;

		int m_state;

		public BuildItemJob(IBuildingObject workplace, ActionPriority priority, IItemObject[] sourceObjects, ItemID dstItemID)
			: base(null, priority)
		{
			m_workplace = workplace;
			m_sourceObjects = sourceObjects;
			m_dstItemID = dstItemID;

			m_state = 0;

			AddSubJob(new FetchItems(this, priority, m_workplace.Environment, m_workplace.Area.Center, sourceObjects));
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.BuildItem(this, this.Priority, m_workplace, m_sourceObjects, m_dstItemID));
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

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}
}
