using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObject(UseRef = true)]
	public class BuildItemJob : JobGroup
	{
		[SaveGameProperty]
		IBuildingObject m_workplace;
		[SaveGameProperty]
		IItemObject[] m_sourceObjects;
		[SaveGameProperty]
		ItemID m_dstItemID;

		[SaveGameProperty]
		int m_state;

		public BuildItemJob(IBuildingObject workplace, IEnumerable<IItemObject> sourceObjects, ItemID dstItemID)
			: base(null)
		{
			m_workplace = workplace;
			m_sourceObjects = sourceObjects.ToArray();
			m_dstItemID = dstItemID;

			foreach (var item in m_sourceObjects)
				item.ReservedBy = this;

			m_state = 0;

			AddSubJob(new FetchItems(this, m_workplace.Environment, m_workplace.Area.Center, sourceObjects));
		}

		protected BuildItemJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStatusChanged(JobStatus status)
		{
			foreach (var item in m_sourceObjects)
				item.ReservedBy = null;

			base.OnStatusChanged(status);
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.MoveBuildItemAssignment(this, m_workplace, m_sourceObjects, m_dstItemID));
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
