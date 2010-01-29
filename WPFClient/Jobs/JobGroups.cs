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
		public BuildItemJob(BuildingData workplace, ItemObject[] sourceObjects)
			: base(null)
		{
			var env = workplace.Environment;
			var location = new IntPoint3D(workplace.Area.TopLeft, workplace.Z);

			AddSubJob(new FetchMaterials(this, env, location, sourceObjects));
			AddSubJob(new BuildItem(this, workplace, sourceObjects));
		}

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}

	class FetchMaterials : ParallelJobGroup
	{
		public FetchMaterials(IJob parent, Environment env, IntPoint3D location, ItemObject[] objects)
			: base(parent)
		{
			foreach (var item in objects)
			{
				AddSubJob(new FetchMaterial(this, env, location, item));
			}
		}

		public override string ToString()
		{
			return "FetchMaterials";
		}
	}
}
