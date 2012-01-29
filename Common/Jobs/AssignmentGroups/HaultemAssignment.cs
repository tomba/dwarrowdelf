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
	public sealed class HaulItemAssignment : AssignmentGroup
	{
		enum State
		{
			None,
			MoveToItem,
			CarryItem,
			Haul,
			DropItem,
		}

		[SaveGameProperty]
		public IItemObject Item { get; private set; }
		[SaveGameProperty]
		IntPoint3 m_location;
		[SaveGameProperty]
		IEnvironmentObject m_environment;
		[SaveGameProperty("State")]
		State m_state;
		[SaveGameProperty]
		DirectionSet m_positioning;

		public HaulItemAssignment(IJobObserver parent, IEnvironmentObject env, IntPoint3 location, IItemObject item)
			: this(parent, env, location, item, DirectionSet.Exact)
		{
		}

		public HaulItemAssignment(IJobObserver parent, IEnvironmentObject env, IntPoint3 location, IItemObject item, DirectionSet positioning)
			: base(parent)
		{
			this.Item = item;
			m_environment = env;
			m_location = location;
			m_state = State.None;
			m_positioning = positioning;
		}

		HaulItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void AssignOverride(ILivingObject worker)
		{
			m_state = State.MoveToItem;
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == State.DropItem)
				SetStatus(JobStatus.Done);
			else
				m_state = m_state + 1;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case State.MoveToItem:
					assignment = new MoveAssignment(this, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case State.CarryItem:
					assignment = new CarryItemAssignment(this, this.Item);
					break;

				case State.Haul:
					assignment = new HaulAssignment(this, m_environment, m_location, m_positioning, this.Item);
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
			return "HaulItemAssignment";
		}
	}
}
