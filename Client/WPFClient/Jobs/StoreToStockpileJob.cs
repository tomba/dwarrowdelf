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
	class StoreToStockpileJob : AssignmentGroup
	{
		enum State
		{
			None = 0,
			MoveToItem,
			GetItem,
			MoveToStockpile,
			MoveToDropLocation,
			DropItem,
			Done,
		}

		public IItemObject Item { get; private set; }
		Stockpile m_stockpile;

		State m_state;

		public StoreToStockpileJob(Stockpile stockpile, IItemObject item)
			: base(null, ActionPriority.Normal)
		{
			this.Item = item;
			m_stockpile = stockpile;
		}

		protected override JobState AssignOverride(ILiving worker)
		{
			m_state = State.MoveToItem;

			return base.AssignOverride(worker);
		}

		protected override IAssignment GetNextAssignment()
		{
			switch (m_state)
			{
				case State.MoveToItem:
					return new MoveAssignment(this, ActionPriority.Normal, this.Item.Environment, this.Item.Location, DirectionSet.Exact);

				case State.GetItem:
					return new GetItemAssignment(this, ActionPriority.Normal, this.Item);

				case State.MoveToStockpile:
					return new MoveAssignment(this, ActionPriority.Normal, m_stockpile.Environment, m_stockpile.Area.Center, DirectionSet.Exact);

				case State.MoveToDropLocation:
					return new MoveAssignment(this, ActionPriority.Normal, m_stockpile.Environment, m_stockpile.FindEmptyLocation, DirectionSet.Exact);

				case State.DropItem:
					return new DropItemAssignment(this, ActionPriority.Normal, this.Item);

				default:
					throw new Exception();
			}
		}

		protected override JobState CheckProgress()
		{
			switch (m_state)
			{
				case State.MoveToItem:
					m_state = State.GetItem;
					break;

				case State.GetItem:
					m_state = State.MoveToStockpile;
					break;

				case State.MoveToStockpile:
					m_state = State.MoveToDropLocation;
					break;

				case State.MoveToDropLocation:
					m_state = State.DropItem;
					break;

				case State.DropItem:
					m_state = State.Done;
					break;

				default:
					throw new Exception();
			}

			if (m_state == State.Done)
				return Jobs.JobState.Done;

			return Jobs.JobState.Ok;
		}

		public override string ToString()
		{
			return "StoreToStockpileJob";
		}
	}
}
