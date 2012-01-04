﻿using System;
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
		[SaveGameProperty]
		public IItemObject Item { get; private set; }
		[SaveGameProperty]
		IntPoint3D m_location;
		[SaveGameProperty]
		IEnvironmentObject m_environment;
		[SaveGameProperty("State")]
		int m_state;

		public HaulItemAssignment(IJobObserver parent, IEnvironmentObject env, IntPoint3D location, IItemObject item)
			: base(parent)
		{
			this.Item = item;
			m_environment = env;
			m_location = location;
			m_state = 0;
		}

		HaulItemAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override void OnAssignmentDone()
		{
			if (m_state == 1)
				SetStatus(JobStatus.Done);
			else
				m_state = m_state + 1;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, this.Item.Environment, this.Item.Location, DirectionSet.Exact);
					break;

				case 1:
					assignment = new MoveAssignment(this, m_environment, m_location, DirectionSet.Exact, this.Item);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "FetchItemAssignment";
		}
	}
}