using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	[SaveGameObject]
	public sealed class InstallItemJob : JobGroup
	{
		[SaveGameProperty]
		IItemObject m_item;
		[SaveGameProperty]
		IntVector3 m_location;

		[SaveGameProperty]
		int m_state;

		public InstallItemJob(IJobObserver parent, IItemObject item, IEnvironmentObject env, IntVector3 location)
			: base(parent)
		{
			m_item = item;
			m_location = location;

			m_state = 0;

			AddSubJob(new AssignmentGroups.FetchItemAssignment(this, env, location, item));
		}

		InstallItemJob(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnStatusChanged(JobStatus status)
		{
			base.OnStatusChanged(status);
		}

		protected override void OnSubJobDone(IJob job)
		{
			RemoveSubJob(job);

			if (m_state == 0)
			{
				m_state = 1;
				AddSubJob(new AssignmentGroups.MoveInstallItemAssignment(this, m_item, InstallMode.Install));
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
			return "InstallItemJob";
		}
	}
}
