using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Jobs
{
	public class RunInCirclesJob : SerialActionJob
	{
		IEnvironment m_environment;

		public RunInCirclesJob(IJob parent, ActionPriority priority, IEnvironment environment)
			: base(parent, priority)
		{
			m_environment = environment;

			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 18, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(14, 18, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(14, 28, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 28, 9), false));
			AddSubJob(new MoveActionJob(this, priority, m_environment, new IntPoint3D(2, 18, 9), false));
		}

		protected override void Cleanup()
		{
			m_environment = null;
		}

		public override string ToString()
		{
			return "RunInCirclesJob";
		}
	}

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


	public class MineAreaJob : SerialActionJob
	{
		public IEnvironment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaJob(IEnvironment env, ActionPriority priority, IntRect rect, int z)
			: base(null, priority)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.Wall);

			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, priority, env, new IntPoint3D(p, z));
				AddSubJob(job);
			}
		}

		protected override void Cleanup()
		{
			m_environment = null;
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialSameJob";
		}
	}

	public class FetchItem : SerialActionJob
	{
		public FetchItem(IJob parent, ActionPriority priority, IEnvironment env, IntPoint3D location, IItemObject item)
			: base(parent, priority)
		{
			AddSubJob(new MoveActionJob(this, priority, item.Environment, item.Location, false));
			AddSubJob(new GetItemActionJob(this, priority, item));
			AddSubJob(new MoveActionJob(this, priority, env, location, false));
			AddSubJob(new DropItemActionJob(this, priority, item));
		}

		public override string ToString()
		{
			return "FetchItem";
		}
	}

	public class BuildItem : SerialActionJob
	{
		public BuildItem(IJob parent, ActionPriority priority, IBuildingObject workplace, IItemObject[] items)
			: base(parent, priority)
		{
			var env = workplace.Environment;
			var p = workplace.Area.X1Y1 + new IntVector(workplace.Area.Width / 2, workplace.Area.Height / 2);
			var location = new IntPoint3D(p, workplace.Z);

			AddSubJob(new MoveActionJob(this, priority, env, location, false));
			AddSubJob(new BuildItemActionJob(this, priority, items));
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
