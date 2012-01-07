using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	sealed class StockpileCriteriaEditable
	{
		public StockpileCriteriaEditable()
		{
			this.ItemIDs = new ObservableCollection<ItemID>();
			this.ItemCategories = new ObservableCollection<ItemCategory>();
			this.MaterialIDs = new ObservableCollection<MaterialID>();
			this.MaterialCategories = new ObservableCollection<MaterialCategory>();
		}

		public StockpileCriteriaEditable(StockpileCriteria source)
		{
			this.ItemIDs = new ObservableCollection<ItemID>(source.ItemIDs);
			this.ItemCategories = new ObservableCollection<ItemCategory>(source.ItemCategories);
			this.MaterialIDs = new ObservableCollection<MaterialID>(source.MaterialIDs);
			this.MaterialCategories = new ObservableCollection<MaterialCategory>(source.MaterialCategories);
		}

		public ObservableCollection<ItemID> ItemIDs { get; set; }
		public ObservableCollection<ItemCategory> ItemCategories { get; set; }
		public ObservableCollection<MaterialID> MaterialIDs { get; set; }
		public ObservableCollection<MaterialCategory> MaterialCategories { get; set; }
	}

	[SaveGameObjectByRef]
	sealed class StockpileCriteria
	{
		public StockpileCriteria()
		{
			this.ItemIDs = new ItemID[0];
			this.ItemCategories = new ItemCategory[0];
			this.MaterialIDs = new MaterialID[0];
			this.MaterialCategories = new MaterialCategory[0];
		}

		public StockpileCriteria(StockpileCriteriaEditable source)
		{
			this.ItemIDs = source.ItemIDs.ToArray();
			this.ItemCategories = source.ItemCategories.ToArray();
			this.MaterialIDs = source.MaterialIDs.ToArray();
			this.MaterialCategories = source.MaterialCategories.ToArray();
		}

		StockpileCriteria(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public ItemID[] ItemIDs { get; set; }
		[SaveGameProperty]
		public ItemCategory[] ItemCategories { get; set; }
		[SaveGameProperty]
		public MaterialID[] MaterialIDs { get; set; }
		[SaveGameProperty]
		public MaterialCategory[] MaterialCategories { get; set; }
	}

	[SaveGameObjectByRef]
	sealed class Stockpile : IDrawableElement, IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }
		IntCuboid IDrawableElement.Area { get { return this.Area.ToCuboid(); } }

		[SaveGameProperty]
		public IntRectZ Area { get; private set; }

		// XXX Just one criteria for now. Could be multiple in the future.
		[SaveGameProperty]
		public StockpileCriteria Criteria { get; private set; }

		[SaveGameProperty]
		List<StoreToStockpileJob> m_jobs;

		public string Description { get { return "Stockpile"; } }

		public Stockpile(EnvironmentObject environment, IntRectZ area)
		{
			this.Environment = environment;
			this.Area = area;
			this.Criteria = new StockpileCriteria();

			m_jobs = new List<StoreToStockpileJob>();

			this.Environment.World.JobManager.AddJobSource(this);
		}

		Stockpile(SaveGameContext ctx)
		{
			this.Environment.World.JobManager.AddJobSource(this);
		}

		[OnSaveGameDeserialized]
		void OnDeserialized()
		{
			foreach (var job in m_jobs)
				GameData.Data.Jobs.Add(job);
		}

		public void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			var jobs = m_jobs.ToArray();

			foreach (var job in jobs)
				job.Abort();

			m_jobs = null;
		}

		public void SetCriteria(StockpileCriteriaEditable criteria)
		{
			this.Criteria = new StockpileCriteria(criteria);
		}

		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			var c = this.Criteria;

			if (c.ItemCategories.Length == 0 && c.ItemIDs.Length == 0 && c.MaterialCategories.Length == 0 && c.MaterialIDs.Length == 0)
				return null;

			var obs = this.Environment.GetContents()
				.OfType<ItemObject>()
				.Where(o => o.IsReserved == false)
				.Where(o => Match(o))
				.Where(o => { var sp = this.Environment.GetStockpileAt(o.Location); return !(sp != null && sp.Match(o)); }); // XXX

			foreach (var ob in obs)
			{
				var job = new StoreToStockpileJob(this, this, ob);

				m_jobs.Add(job);

				GameData.Data.Jobs.Add(job);

				ob.ReservedBy = this;

				return job;
			}

			return null;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var j = (StoreToStockpileJob)job;

			if (status == JobStatus.Ok)
				throw new Exception();

			if (status != JobStatus.Done)
				GameData.Data.AddGameEvent(j.Item, "failed to store item to stockpile");

			Debug.Assert(j.Item.ReservedBy == this);
			j.Item.ReservedBy = null;

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
			var c = this.Criteria;

			Debug.Assert(c.ItemCategories != null || c.ItemIDs != null || c.MaterialCategories != null || c.MaterialIDs != null);

			if (c.ItemCategories.Length == 0 && c.ItemIDs.Length == 0 && c.MaterialCategories.Length == 0 && c.MaterialIDs.Length == 0)
				return false;

			if (c.ItemCategories.Length != 0 && c.ItemCategories.Contains(item.ItemCategory) == false)
				return false;

			if (c.ItemIDs.Length != 0 && c.ItemIDs.Contains(item.ItemID) == false)
				return false;

			if (c.MaterialCategories.Length != 0 && c.MaterialCategories.Contains(item.MaterialCategory) == false)
				return false;

			if (c.MaterialIDs.Length != 0 && c.MaterialIDs.Contains(item.MaterialID) == false)
				return false;

			return true;
		}

		public override string ToString()
		{
			return String.Format("Stockpile");
		}
	}
}
