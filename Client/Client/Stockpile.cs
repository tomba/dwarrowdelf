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
		[SaveGameProperty]
		public ItemID[] ItemIDs { get; set; }
		[SaveGameProperty]
		public ItemCategory[] ItemCategories { get; set; }
		[SaveGameProperty]
		public MaterialID[] MaterialIDs { get; set; }
		[SaveGameProperty]
		public MaterialCategory[] MaterialCategories { get; set; }

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

		public bool IsEmpty
		{
			get
			{
				return this.ItemIDs.Length == 0 && this.ItemCategories.Length == 0 && this.MaterialIDs.Length == 0 && this.MaterialCategories.Length == 0;
			}
		}
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

		ItemObjectView m_itemObjectView;

		public Stockpile(EnvironmentObject environment, IntRectZ area)
		{
			this.Environment = environment;
			this.Area = area;
			this.Criteria = new StockpileCriteria();

			m_jobs = new List<StoreToStockpileJob>();

			this.Environment.World.JobManager.AddJobSource(this);

			this.Environment.ObjectRemoved += new Action<MovableObject>(Environment_ObjectRemoved);
			this.Environment.ObjectMoved += new Action<MovableObject, IntPoint3D>(Environment_ObjectMoved);

			m_itemObjectView = new ItemObjectView(this.Environment, this.Area.Center,
				o => o.IsReserved == false && o.IsStockpiled == false && Match(o));
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

			if (m_itemObjectView.IsEnabled)
				DisableItemObjectView();

			foreach (var item in this.Area.Range().SelectMany(p => this.Environment.GetContents(p)).OfType<ItemObject>())
			{
				if (item.StockpiledBy == this)
					item.StockpiledBy = null;
			}
		}

		void Environment_ObjectMoved(MovableObject ob, IntPoint3D oldPos)
		{
			if (this.Area.Contains(oldPos) == false)
				return;

			var item = ob as ItemObject;

			if (item != null && item.StockpiledBy == this)
				item.StockpiledBy = null;
		}

		void Environment_ObjectRemoved(MovableObject ob)
		{
			if (this.Area.Contains(ob.Location) == false)
				return;

			var item = ob as ItemObject;

			if (item != null && item.StockpiledBy == this)
				item.StockpiledBy = null;
		}

		public void SetCriteria(StockpileCriteriaEditable criteria)
		{
			this.Criteria = new StockpileCriteria(criteria);

			if (this.Criteria.IsEmpty == false && m_itemObjectView.IsEnabled == false)
				EnableItemObjectView();
			else if (this.Criteria.IsEmpty && m_itemObjectView.IsEnabled)
				DisableItemObjectView();
		}

		void EnableItemObjectView()
		{
			m_itemObjectView.Enable();

			ItemObject.IsReservedChanged += ItemObject_IsReservedChanged;
			ItemObject.IsStockpiledChanged += ItemObject_IsStockpiledChanged;
		}

		void DisableItemObjectView()
		{
			m_itemObjectView.Disable();

			ItemObject.IsReservedChanged -= ItemObject_IsReservedChanged;
			ItemObject.IsStockpiledChanged -= ItemObject_IsStockpiledChanged;
		}

		void ItemObject_IsStockpiledChanged(ItemObject ob)
		{
			m_itemObjectView.Update(ob);
		}

		void ItemObject_IsReservedChanged(ItemObject ob)
		{
			m_itemObjectView.Update(ob);
		}



		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			if (m_itemObjectView.IsEnabled == false)
				return null;

			var ob = m_itemObjectView.GetFirst();

			if (ob == null)
				return null;

			var job = new StoreToStockpileJob(this, this, ob);

			m_jobs.Add(job);

			GameData.Data.Jobs.Add(job);

			ob.ReservedBy = this;

			return job;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var j = (StoreToStockpileJob)job;

			switch (status)
			{
				case JobStatus.Done:
					j.Item.StockpiledBy = this;
					break;

				case JobStatus.Abort:
				case JobStatus.Fail:
					GameData.Data.AddGameEvent(j.Item, "failed to store item to stockpile");
					break;

				default:
					throw new Exception();
			}

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

			if (c.IsEmpty)
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
