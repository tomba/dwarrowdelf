using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	class MoveMineJob : SerialActionJob
	{
		Environment m_environment;
		IntPoint3D m_location;

		public MoveMineJob(IJob parent, Environment environment, IntPoint3D location)
			: base(parent)
		{
			m_environment = environment;
			m_location = location;

			AddSubJob(new MoveActionJob(this, m_environment, m_location, true));
			AddSubJob(new MineActionJob(this, m_environment, m_location));
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


	class MineAreaJob : SerialActionJob
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.Wall);

			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, env, new IntPoint3D(p, z));
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

	class FetchItem : SerialActionJob
	{
		public FetchItem(IJob parent, Environment env, IntPoint3D location, ItemObject item)
			: base(parent)
		{
			AddSubJob(new MoveActionJob(this, item.Environment, item.Location, false));
			AddSubJob(new GetItemActionJob(this, item));
			AddSubJob(new MoveActionJob(this, env, location, false));
			AddSubJob(new DropItemActionJob(this, item));
		}

		public override string ToString()
		{
			return "FetchItem";
		}
	}

	class BuildItem : SerialActionJob
	{
		public BuildItem(IJob parent, BuildingObject workplace, ItemObject[] items)
			: base(parent)
		{
			var env = workplace.Environment;
			var p = workplace.Area.X1Y1 + new IntVector(workplace.Area.Width / 2, workplace.Area.Height / 2);
			var location = new IntPoint3D(p, workplace.Z);

			AddSubJob(new MoveActionJob(this, env, location, false));
			AddSubJob(new BuildItemActionJob(this, items));
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
