using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs.JobGroups
{
	public class BuildItemJob : SerialJobGroup
	{
		public BuildItemJob(IBuildingObject workplace, ActionPriority priority, IItemObject[] sourceObjects)
			: base(null, priority)
		{
			var env = workplace.Environment;
			var p = workplace.Area.X1Y1 + new IntVector(workplace.Area.Width / 2, workplace.Area.Height / 2);
			var location = new IntPoint3D(p, workplace.Z);

			AddSubJob(new FetchItems(this, priority, env, location, sourceObjects));
			AddSubJob(new AssignmentGroups.BuildItem(this, priority, workplace, sourceObjects));
		}

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}
}
