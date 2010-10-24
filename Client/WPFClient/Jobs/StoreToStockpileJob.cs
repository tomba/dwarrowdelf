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
				new MoveAssignment(this, ActionPriority.Normal, item.Environment, item.Location, false),
				new GetItemAssignment(this, ActionPriority.Normal, item),
				new MoveAssignment(this, ActionPriority.Normal, stockpile.Environment, stockpile.Area.Center, false),
				new MoveAssignment(this, ActionPriority.Normal, stockpile.Environment, stockpile.FindEmptyLocation, false),
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
