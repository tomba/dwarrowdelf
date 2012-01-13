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
	public sealed class LoiterAssignment : AssignmentGroup
	{
		[SaveGameProperty("Environment")]
		readonly IEnvironmentObject m_environment;
		[SaveGameProperty("State")]
		int m_state;

		[SaveGameProperty]
		IntPoint3[] m_corners;

		public LoiterAssignment(IJobObserver parent, IEnvironmentObject environment)
			: base(parent)
		{
			m_environment = environment;

			m_corners = new IntPoint3[4];

			m_corners[0] = FindCorner(m_environment, m_environment.HomeLocation, new IntVector3(-10, -10, 0));
			m_corners[1] = FindCorner(m_environment, m_environment.HomeLocation, new IntVector3(10, -10, 0));
			m_corners[2] = FindCorner(m_environment, m_environment.HomeLocation, new IntVector3(10, 10, 0));
			m_corners[3] = FindCorner(m_environment, m_environment.HomeLocation, new IntVector3(-10, 10, 0));

			m_state = 0;
		}

		LoiterAssignment(SaveGameContext ctx)
			: base(ctx)
		{
		}

		static IntPoint3 FindCorner(IEnvironmentObject env, IntPoint3 hl, IntVector3 v)
		{
			IntPoint3 p = hl + v;

			int steps = Math.Max(Math.Abs(v.X), Math.Abs(v.Y));

			double x = p.X;
			double y = p.Y;

			double xd = (double)v.X / steps;
			double yd = (double)v.Y / steps;

			for (int i = 0; i < steps; ++i)
			{
				if (env.CanEnter(p))
					return p;

				x -= xd;
				y -= yd;

				p = new IntPoint3((int)x, (int)y, p.Z);
			}

			throw new Exception();
		}

		protected override void OnAssignmentDone()
		{
			m_state = (m_state + 1) % 4;
		}

		protected override IAssignment PrepareNextAssignment()
		{
			IAssignment assignment;

			switch (m_state)
			{
				case 0:
					assignment = new MoveAssignment(this, m_environment, m_corners[0], DirectionSet.Exact);
					break;

				case 1:
					assignment = new MoveAssignment(this, m_environment, m_corners[1], DirectionSet.Exact);
					break;

				case 2:
					assignment = new MoveAssignment(this, m_environment, m_corners[2], DirectionSet.Exact);
					break;

				case 3:
					assignment = new MoveAssignment(this, m_environment, m_corners[3], DirectionSet.Exact);
					break;

				default:
					throw new Exception();
			}

			return assignment;
		}

		public override string ToString()
		{
			return "LoiterAssignment";
		}
	}
}
