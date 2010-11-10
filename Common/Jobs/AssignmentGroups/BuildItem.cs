using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.Assignments;

namespace Dwarrowdelf.Jobs.AssignmentGroups
{
	public class BuildItem : StaticAssignmentGroup
	{
		public BuildItem(IJob parent, ActionPriority priority, IBuildingObject workplace, IItemObject[] items, ItemType dstItemID)
			: base(parent, priority)
		{
			var env = workplace.Environment;
			var location = workplace.Area.Center;

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, env, location, DirectionSet.Exact),
				new BuildItemAssignment(this, priority, items, dstItemID),
			});
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
