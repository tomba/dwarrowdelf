using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef]
	sealed class StockpileCriteria
	{
		public StockpileCriteria()
		{
			this.ItemIDs = new ObservableCollection<ItemID>();
			this.ItemCategories = new ObservableCollection<ItemCategory>();
			this.MaterialIDs = new ObservableCollection<MaterialID>();
			this.MaterialCategories = new ObservableCollection<MaterialCategory>();
		}

		StockpileCriteria(SaveGameContext ctx)
		{
		}

		[SaveGameProperty]
		public ObservableCollection<ItemID> ItemIDs { get; private set; }
		[SaveGameProperty]
		public ObservableCollection<ItemCategory> ItemCategories { get; private set; }
		[SaveGameProperty]
		public ObservableCollection<MaterialID> MaterialIDs { get; private set; }
		[SaveGameProperty]
		public ObservableCollection<MaterialCategory> MaterialCategories { get; private set; }
		// quality
	}

	[SaveGameObjectByRef]
	sealed class Stockpile : IDrawableElement, IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }
		IntCuboid IDrawableElement.Area { get { return this.Area.ToCuboid(); } }

		[SaveGameProperty]
		public IntRectZ Area { get; private set; }

		[SaveGameProperty]
		public ObservableCollection<StockpileCriteria> Criterias { get; private set; }

		[SaveGameProperty]
		List<StoreToStockpileJob> m_jobs;

		public string Description { get { return "Stockpile"; } }

		public Stockpile(EnvironmentObject environment, IntRectZ area)
		{
			this.Environment = environment;
			this.Area = area;
			this.Criterias = new ObservableCollection<StockpileCriteria>();

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

		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			if (this.Criterias.Count == 0)
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
