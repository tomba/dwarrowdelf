using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	class MineAreaParallelJob : ParallelJobGroup
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaParallelJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.NaturalWall);

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

	class MineAreaSerialJob : SerialJobGroup
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaSerialJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.NaturalWall);

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

	class BuildItemJob : SerialJobGroup
	{
		public BuildItemJob(BuildingObject workplace, ItemObject[] sourceObjects)
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

	class FetchItems : ParallelJobGroup
	{
		public FetchItems(IJob parent, Environment env, IntPoint3D location, ItemObject[] items)
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
