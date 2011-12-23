using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	[SaveGameObjectByRef]
	public sealed class MoveBuildItemAssignment : MoveBaseAssignment
	{
		[SaveGameProperty]
		IBuildingObject m_workplace;
		[SaveGameProperty]
		IItemObject[] m_items;
		[SaveGameProperty]
		ItemID m_dstItemID;

		public MoveBuildItemAssignment(IJobObserver parent, IBuildingObject workplace, IItemObject[] items, ItemID dstItemID)
			: base(parent, workplace.Environment, workplace.Area.Center)
		{
			m_workplace = workplace;
			m_items = items;
			m_dstItemID = dstItemID;
		}

		MoveBuildItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override DirectionSet GetPositioning()
		{
			return DirectionSet.Exact;
		}

		protected override IAssignment CreateAssignment()
		{
			return new BuildItemAssignment(this, m_items, m_dstItemID);
		}

		public override string ToString()
		{
			return "MoveBuildItemAssignment";
		}
	}

}
