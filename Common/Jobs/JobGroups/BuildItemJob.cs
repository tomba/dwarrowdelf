using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObjectByRef]
	public sealed class BuildItemJob : JobGroup
	{
		[SaveGameProperty]
		IBuildingObject m_workplace;
		[SaveGameProperty]
		IItemObject[] m_sourceObjects;
		[SaveGameProperty]
		string m_buildableItemKey;

		[SaveGameProperty]
		int m_state;

		public BuildItemJob(IJobObserver parent, IBuildingObject workplace, string buildableItemKey, IEnumerable<IItemObject> sourceObjects)
			: base(parent)
		{
			m_workplace = workplace;
			m_sourceObjects = sourceObjects.ToArray();
			m_buildableItemKey = buildableItemKey;

			m_state = 0;

			AddSubJob(new FetchItems(this, m_workplace.Environment, m_workplace.Area.Center, sourceObjects));
		}

		BuildItemJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.MoveBuildItemAssignment(this, m_workplace, m_buildableItemKey, m_sourceObjects));
			}
			else if (m_state == 1)
			{
				SetStatus(JobStatus.Done);
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
