using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Dwarrowdelf.Jobs.ActionJobs;

namespace Dwarrowdelf.Jobs.SerialActionJobs
{
	public class MoveMineJob : SerialActionJob
	{
		IEnvironment m_environment;
		IntPoint3D m_location;

		public MoveMineJob(IJob parent, ActionPriority priority, IEnvironment environment, IntPoint3D location)
			: base(parent, priority)
		{
			m_environment = environment;
			m_location = location;

			AddSubJob(new MoveActionJob(this, priority, m_environment, m_location, true));
			AddSubJob(new MineActionJob(this, priority, m_environment, m_location));
		}

		/*
		 * XXX checkvalidity tms
		protected override Progress AssignOverride(Living worker)
		{
			if (worker.Environment != m_environment)
				return Progress.Abort;

			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;

			return Progress.Ok;
		}
		*/

		protected override void Cleanup()
		{
			m_environment = null;
		}

		public override string ToString()
		{
			return "MoveMineJob";
		}
	}
}
