using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client
{
	class StockpileCriteria
	{
		public StockpileCriteria()
		{
			this.ItemIDs = new ObservableCollection<ItemID>();
			this.ItemCategories = new ObservableCollection<ItemCategory>();
			this.MaterialIDs = new ObservableCollection<MaterialID>();
			this.MaterialCategories = new ObservableCollection<MaterialCategory>();
		}

		public ObservableCollection<ItemID> ItemIDs { get; private set; }
		public ObservableCollection<ItemCategory> ItemCategories { get; private set; }
		public ObservableCollection<MaterialID> MaterialIDs { get; private set; }
		public ObservableCollection<MaterialCategory> MaterialCategories { get; private set; }
		// quality
	}

	[SaveGameObjectByRef]
	class Stockpile : IDrawableElement, IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public Environment Environment { get; private set; }
		IntCuboid IDrawableElement.Area { get { return this.Area.ToCuboid(); } }

		[SaveGameProperty]
		public IntRectZ Area { get; private set; }

		public ObservableCollection<StockpileCriteria> Criterias { get; private set; }

		List<StoreToStockpileJob> m_jobs = new List<StoreToStockpileJob>();

		FrameworkElement m_element;
		public FrameworkElement Element { get { return m_element; } }

		public string Description { get { return "Stockpile"; } }

		public Stockpile(Environment environment, IntRectZ area)
		{
			this.Environment = environment;
			this.Area = area;
			this.Criterias = new ObservableCollection<StockpileCriteria>();

			var rect = new Rectangle();
			rect.Stroke = Brushes.Gray;
			rect.StrokeThickness = 0.1;
			rect.Width = area.Width;
			rect.Height = area.Height;
			rect.IsHitTestVisible = false;
			m_element = rect;

			this.Environment.World.JobManager.AddJobSource(this);
		}

		Stockpile(SaveGameContext ctx)
		{
		}

		public void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			var jobs = m_jobs.ToArray();

			foreach (var job in jobs)
				job.Abort();

			m_jobs = null;
		}

		IAssignment IJobSource.FindAssignment(ILiving living)
		{
			if (this.Criterias.Count == 0)
				return null;

			var obs = this.Environment.GetContents()
				.OfType<ItemObject>()
				.Where(o => o.ReservedBy == null)
				.Where(o => Match(o))
				.Where(o => { var sp = this.Environment.GetStockpileAt(o.Location); return !(sp != null && sp.Match(o)); }); // XXX

			foreach (var ob in obs)
			{
				var job = new StoreToStockpileJob(this, this, ob);

				m_jobs.Add(job);

				GameData.Data.Jobs.Add(job);

				return job;
			}

			return null;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var j = (StoreToStockpileJob)job;

			if (status == JobStatus.Ok)
				throw new Exception();

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
			foreach (var c in this.Criterias)
			{
				Debug.Assert(c.ItemCategories != null || c.ItemIDs != null || c.MaterialCategories != null || c.MaterialIDs != null);

				if (c.ItemCategories.Count == 0 && c.ItemIDs.Count == 0 && c.MaterialCategories.Count == 0 && c.MaterialIDs.Count == 0)
					continue;

				if (c.ItemCategories.Count != 0 && c.ItemCategories.Contains(item.ItemCategory) == false)
					continue;

				if (c.ItemIDs.Count != 0 && c.ItemIDs.Contains(item.ItemID) == false)
					continue;

				if (c.MaterialCategories.Count != 0 && c.MaterialCategories.Contains(item.MaterialCategory) == false)
					continue;

				if (c.MaterialIDs.Count != 0 && c.MaterialIDs.Contains(item.MaterialID) == false)
					continue;

				return true;
			}

			return false;
		}

		public override string ToString()
		{
			return String.Format("Stockpile");
		}
	}
}
