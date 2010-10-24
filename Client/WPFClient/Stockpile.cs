using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	[Flags]
	enum StockpileType
	{
		None = 0,
		Logs = 1 << 0,
		Gems = 1 << 1,
		Rocks = 1 << 2,
		Metals = 1 << 3,
	}

	// XXX should be configurable for classes, subclasses and certain items...

	class Stockpile : IDrawableArea
	{
		public Environment Environment { get; private set; }
		IntCuboid IDrawableArea.Area { get { return this.Area.ToCuboid(); } }
		public System.Windows.Media.Brush Fill { get { return null; } }
		public double Opacity { get { return 1.0; } }

		public IntRect3D Area { get; private set; }

		public StockpileType StockpileType { get; private set; }

		List<StoreToStockpileJob> m_jobs = new List<StoreToStockpileJob>();

		public Stockpile(Environment environment, IntRect3D area, StockpileType stockpileType)
		{
			this.Environment = environment;
			this.Area = area;
			this.StockpileType = stockpileType;

			this.Environment.World.TickStartEvent += World_TickStartEvent;
		}

		public void Destruct()
		{
			this.Environment.World.TickStartEvent -= World_TickStartEvent;

			foreach (var job in m_jobs)
			{
				job.Item.ReservedBy = null;
				job.StateChanged -= OnJobStateChanged;
				this.Environment.World.JobManager.Remove(job);
			}

			m_jobs = null;
		}

		void World_TickStartEvent()
		{
			Check();
		}

		void Check()
		{
			int numOfHaulersFree = 1;

			var obs = this.Environment.GetContents()
				.OfType<ItemObject>()
				.Where(o => o.ReservedBy == null)
				.Where(o => Match(o))
				.Where(o => { var sp = this.Environment.GetStockpileAt(o.Location); return !(sp != null && sp.Match(o)); }) // XXX
				.Take(numOfHaulersFree);

			foreach (var ob in obs)
			{
				var job = new StoreToStockpileJob(this, ob);

				ob.ReservedBy = this;
				job.StateChanged += OnJobStateChanged;
				this.Environment.World.JobManager.Add(job);
				m_jobs.Add(job);
			}
		}

		public IntPoint3D FindEmptyLocation(out bool ok)
		{
			var env = this.Environment;
			

			for (int i = 0; i < 10; ++i)
			{
				var loc = this.Area.Range().FirstOrDefault(p => env.GetContents(p).OfType<ItemObject>().Count() == i);

				if (loc != new IntPoint3D())
				{
					ok = true;
					return loc;
				}
			}

			ok = false;
			return new IntPoint3D();
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			var j = (StoreToStockpileJob)job;

			if (state == JobState.Done)
			{
				j.Item.ReservedBy = null;
				job.StateChanged -= OnJobStateChanged;
				this.Environment.World.JobManager.Remove(job);
				m_jobs.Remove(j);
			}
		}

		bool Match(ItemObject item)
		{
			StockpileType types = this.StockpileType;

			if ((types & StockpileType.Logs) != 0 && item.ItemClass == ItemClass.RawMaterial && item.MaterialClass == MaterialClass.Wood)
				return true;

			if ((types & StockpileType.Gems) != 0 && item.ItemClass == ItemClass.Gem && item.MaterialClass == MaterialClass.Gem)
				return true;

			if ((types & StockpileType.Rocks) != 0 && item.ItemClass == ItemClass.RawMaterial && item.MaterialClass == MaterialClass.Rock)
				return true;

			if ((types & StockpileType.Metals) != 0 && item.ItemClass == ItemClass.RawMaterial && item.MaterialClass == MaterialClass.Metal)
				return true;

			return false;
		}
	}
}
