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
		public BuildItem(IJob parent, ActionPriority priority, IBuildingObject workplace, IItemObject[] items)
			: base(parent, priority)
		{
			var env = workplace.Environment;
			var p = workplace.Area.X1Y1 + new IntVector(workplace.Area.Width / 2, workplace.Area.Height / 2);
			var location = new IntPoint3D(p, workplace.Z);

			SetAssignments(new IAssignment[] {
				new MoveAssignment(this, priority, env, location, false),
				new BuildItemAssignment(this, priority, items),
			});
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
