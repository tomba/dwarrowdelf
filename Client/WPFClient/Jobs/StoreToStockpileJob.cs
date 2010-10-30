using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.Assignments;
using Dwarrowdelf.Jobs.AssignmentGroups;

namespace Dwarrowdelf.Client
{
	class StoreToStockpileJob : StaticAssignmentGroup
	{
		public IItemObject Item { get; private set; }

		public StoreToStockpileJob(Stockpile stockpile, IItemObject item)
			: base(null, ActionPriority.Normal)
		{
			this.Item = item;

			var jobs = new IAssignment[] {
				new MoveAssignment(this, ActionPriority.Normal, item.Environment, item.Location, Positioning.Exact),
				new GetItemAssignment(this, ActionPriority.Normal, item),
				new MoveAssignment(this, ActionPriority.Normal, stockpile.Environment, stockpile.Area.Center, Positioning.Exact),
				new MoveAssignment(this, ActionPriority.Normal, stockpile.Environment, stockpile.FindEmptyLocation, Positioning.Exact),
				new DropItemAssignment(this, ActionPriority.Normal, item),
			};

			SetAssignments(jobs);
		}

		public override string ToString()
		{
			return "StoreToStockpileJob";
		}
	}
}
