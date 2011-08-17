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

		public ItemObject Item { get; private set; }
		Stockpile m_stockpile;
		State m_state;

		public StoreToStockpileJob(Stockpile stockpile, ItemObject item)
			: base(null)
		{
			this.Item = item;
			m_stockpile = stockpile;
		}

		protected override JobStatus AssignOverride(ILiving worker)
		{
			m_state = State.MoveToItem;
			return JobStatus.Ok;
		}

		protected override void OnAssignmentDone()
		{
			switch (m_state)
			{
				case State.MoveToItem:
					Debug.Assert(this.Item.Location == this.Worker.Location);

					m_state = State.GetItem;
					break;

				case State.GetItem:
					Debug.Assert(this.Item.Parent == this.Worker);

					m_state = State.MoveToStockpile;
					break;

				case State.MoveToStockpile:
					if (m_stockpile.Area.Contains(this.Worker.Location) && m_stockpile.LocationOk(this.Worker.Location, this.Item))
						m_state = State.DropItem;
					break;

				case State.DropItem:
					m_state = State.Done;
					SetStatus(JobStatus.Done);
					break;

				default:
					throw new Exception();
			}
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case State.MoveToItem:
					assignment = new MoveAssignment(this, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case State.GetItem:
					assignment = new GetItemAssignment(this, this.Item);
					break;

				case State.MoveToStockpile:
					if (!m_stockpile.Area.Contains(this.Worker.Location))
					{
						assignment = new MoveToAreaAssignment(this, m_stockpile.Environment, m_stockpile.Area.ToCuboid(), DirectionSet.Exact);
					}
					else
					{
						bool ok;
						var l = m_stockpile.FindEmptyLocation(out ok);
						if (!ok)
							throw new Exception();

						assignment = new MoveAssignment(this, m_stockpile.Environment, l, DirectionSet.Exact);
					}
					break;

				case State.DropItem:
					assignment = new DropItemAssignment(this, this.Item);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "StoreToStockpileJob";
		}
	}
}
