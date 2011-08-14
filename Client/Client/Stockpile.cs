using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

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
		Furniture = 1 << 4,
	}

	// XXX should be configurable for classes, subclasses and certain items...

	class Stockpile : IDrawableElement, IJobSource
	{
		public Environment Environment { get; private set; }
		IntCuboid IDrawableElement.Area { get { return this.Area.ToCuboid(); } }

		public IntRectZ Area { get; private set; }

		public StockpileType StockpileType { get; private set; }

		List<StoreToStockpileJob> m_jobs = new List<StoreToStockpileJob>();

		FrameworkElement m_element;
		public FrameworkElement Element { get { return m_element; } }

		public string Description { get { return "Stockpile"; } }

		public Stockpile(Environment environment, IntRectZ area, StockpileType stockpileType)
		{
			this.Environment = environment;
			this.Area = area;
			this.StockpileType = stockpileType;

			var rect = new Rectangle();
			rect.Stroke = Brushes.Gray;
			rect.StrokeThickness = 0.1;
			rect.Width = area.Width;
			rect.Height = area.Height;
			rect.IsHitTestVisible = false;
			m_element = rect;

			this.Environment.World.JobManager.AddJobSource(this);
		}

		public void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			foreach (var job in m_jobs)
			{
				job.Item.ReservedBy = null;
				job.StatusChanged -= OnJobStatusChanged;
				GameData.Data.Jobs.Remove(job);
			}

			m_jobs = null;
		}

		bool IJobSource.HasWork
		{
			get
			{
				return true; // XXX
			}
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			var obs = this.Environment.GetContents()
				.OfType<ItemObject>()
				.Where(o => o.ReservedBy == null)
				.Where(o => Match(o))
				.Where(o => { var sp = this.Environment.GetStockpileAt(o.Location); return !(sp != null && sp.Match(o)); }); // XXX

			foreach (var ob in obs)
			{
				var job = new StoreToStockpileJob(this, ob);

				job.Item.ReservedBy = this;
				job.StatusChanged += OnJobStatusChanged;
				m_jobs.Add(job);

				GameData.Data.Jobs.Add(job);

				yield return job;
			}
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			var j = (StoreToStockpileJob)job;

			if (status == JobStatus.Ok)
				throw new Exception();

			j.Item.ReservedBy = null;
			job.StatusChanged -= OnJobStatusChanged;
			m_jobs.Remove(j);

			GameData.Data.Jobs.Remove(j);
		}

		// XXX Silly algorithm. Fill the stockpile evenly.
		public IntPoint3D FindEmptyLocation(out bool ok)
		{
			var env = this.Environment;

			int min = GetMinStack();

			var loc = this.Area.Range().FirstOrDefault(p => GetStack(p) == min);

			if (loc != new IntPoint3D())
			{
				ok = true;
				return loc;
			}

			ok = false;
			return new IntPoint3D();
		}

		public bool LocationOk(IntPoint3D p, ItemObject ob)
		{
			if (!this.Area.Contains(p))
				throw new Exception();

			int min = GetMinStack();

			return GetStack(p) == GetMinStack();
		}

		int GetMinStack()
		{
			return this.Area.Range().Min(p => GetStack(p));
		}

		int GetStack(IntPoint3D p)
		{
			return this.Environment.GetContents(p).OfType<ItemObject>().Count();
		}

		bool Match(ItemObject item)
		{
			StockpileType types = this.StockpileType;

			if ((types & StockpileType.Logs) != 0 && item.ItemCategory == ItemCategory.RawMaterial && item.MaterialCategory == MaterialCategory.Wood)
				return true;

			if ((types & StockpileType.Gems) != 0 && item.ItemCategory == ItemCategory.Gem && item.MaterialCategory == MaterialCategory.Gem)
				return true;

			if ((types & StockpileType.Rocks) != 0 && item.ItemCategory == ItemCategory.RawMaterial && item.MaterialCategory == MaterialCategory.Rock)
				return true;

			if ((types & StockpileType.Metals) != 0 && item.ItemCategory == ItemCategory.RawMaterial && item.MaterialCategory == MaterialCategory.Metal)
				return true;

			if ((types & StockpileType.Furniture) != 0 && item.ItemCategory == ItemCategory.Furniture)
				return true;

			return false;
		}

		public override string ToString()
		{
			return String.Format("Stockpile({0})", this.StockpileType);
		}
	}
}
