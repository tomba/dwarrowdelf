using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame.Jobs
{
	public class MineAreaParallelJob : ParallelJobGroup
	{
		public IEnvironment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaParallelJob(IEnvironment env, IntRect rect, int z)
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
			return "MineAreaParallelJob";
		}
	}

	public class MineAreaSerialJob : SerialJobGroup
	{
		public IEnvironment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaSerialJob(IEnvironment env, IntRect rect, int z)
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
			return "MineAreaSerialJob";
		}
	}

	public class BuildItemJob : SerialJobGroup
	{
		public BuildItemJob(IBuildingObject workplace, IItemObject[] sourceObjects)
			: base(null)
		{
			var env = workplace.Environment;
			var p = workplace.Area.X1Y1 + new IntVector(workplace.Area.Width / 2, workplace.Area.Height / 2);
			var location = new IntPoint3D(p, workplace.Z);

			AddSubJob(new FetchItems(this, env, location, sourceObjects));
			AddSubJob(new BuildItem(this, workplace, sourceObjects));
		}

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}

	public class FetchItems : ParallelJobGroup
	{
		public FetchItems(IJob parent, IEnvironment env, IntPoint3D location, IItemObject[] items)
			: base(parent)
		{
			foreach (var item in items)
			{
				AddSubJob(new FetchItem(this, env, location, item));
			}
		}

		public override string ToString()
		{
			return "FetchItems";
		}
	}
}
