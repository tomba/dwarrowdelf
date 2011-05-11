using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.Assignments;
using Dwarrowdelf.Jobs.AssignmentGroups;
using System.Diagnostics;

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

		protected override void AssignOverride(ILiving worker)
		{
			SetStatus(JobState.Ok);

			SetState(State.MoveToItem);
		}

		void SetState(State state)
		{
			IAssignment assignment;

			switch (state)
			{
				case State.MoveToItem:
					assignment = new MoveAssignment(this, ActionPriority.Normal, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case State.GetItem:
					assignment = new GetItemAssignment(this, ActionPriority.Normal, this.Item);
					break;

				case State.MoveToStockpile:
					if (!m_stockpile.Area.Contains(this.Worker.Location))
					{
						assignment = new MoveToAreaAssignment(this, ActionPriority.Normal, m_stockpile.Environment, m_stockpile.Area.ToCuboid(), DirectionSet.Exact);
					}
					else
					{
						bool ok;
						var l = m_stockpile.FindEmptyLocation(out ok);
						if (!ok)
							throw new Exception();

						assignment = new MoveAssignment(this, ActionPriority.Normal, m_stockpile.Environment, l, DirectionSet.Exact);
					}
					break;

				case State.DropItem:
					assignment = new DropItemAssignment(this, ActionPriority.Normal, this.Item);
					break;

				case State.Done:
					assignment = null;
					SetStatus(JobState.Done);
					break;

				default:
					throw new Exception();
			}

			m_state = state;

			SetAssignment(assignment);
		}

		protected override void OnAssignmentStateChanged(JobState jobState)
		{
			if (jobState == Jobs.JobState.Ok)
				return;

			if (jobState == Jobs.JobState.Fail)
			{
				SetStatus(JobState.Fail);
				return;
			}

			if (jobState == Jobs.JobState.Abort)
			{
				SetStatus(Jobs.JobState.Abort); // XXX check why the job aborted, and possibly retry
				return;
			}

			// else Done

			switch (m_state)
			{
				case State.MoveToItem:
					Debug.Assert(this.Item.Location == this.Worker.Location);

					SetState(State.GetItem);
					break;

				case State.GetItem:
					Debug.Assert(this.Item.Parent == this.Worker);

					SetState(State.MoveToStockpile);
					break;

				case State.MoveToStockpile:
					SetState(State.DropItem);
					break;

				case State.DropItem:
					SetState(State.Done);
					break;

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			return "StoreToStockpileJob";
		}
	}
}
