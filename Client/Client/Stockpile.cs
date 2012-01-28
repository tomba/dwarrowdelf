using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef]
	sealed class Stockpile : IAreaElement, IJobSource, IJobObserver
	{
		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }

		[SaveGameProperty]
		public IntRectZ Area { get; private set; }

		// XXX Just one criteria for now. Could be multiple in the future.
		[SaveGameProperty]
		public IItemMaterialFilter Criteria { get; private set; }

		[SaveGameProperty]
		List<StoreToStockpileJob> m_jobs;

		public string Description { get { return "Stockpile"; } }
		public SymbolID SymbolID { get { return Client.SymbolID.Contraption; } }
		public GameColor EffectiveColor { get { return GameColor.Gray; } }

		TargetItemTracker m_itemTracker;

		public Stockpile(EnvironmentObject environment, IntRectZ area)
		{
			this.Environment = environment;
			this.Area = area;
			this.Criteria = null;

			m_jobs = new List<StoreToStockpileJob>();

			this.Environment.World.JobManager.AddJobSource(this);

			this.Environment.ObjectRemoved += new Action<MovableObject>(Environment_ObjectRemoved);
			this.Environment.ObjectMoved += new Action<MovableObject, IntPoint3>(Environment_ObjectMoved);

			m_itemTracker = new TargetItemTracker(this.Environment, this.Area.Center,
				o => o.IsReserved == false && o.IsStockpiled == false && o.IsInstalled == false && Match(o));
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

			if (m_itemTracker.IsEnabled)
				DisableItemObjectView();

			foreach (var item in this.Area.Range().SelectMany(p => this.Environment.GetContents(p)).OfType<ItemObject>())
			{
				if (item.StockpiledBy == this)
					item.StockpiledBy = null;
			}
		}

		void Environment_ObjectMoved(MovableObject ob, IntPoint3 oldPos)
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

		public void SetCriteria(IItemMaterialFilter itemFilter)
		{
			this.Criteria = itemFilter;

			foreach (var ob in this.Environment.GetContents(this.Area).OfType<ItemObject>())
			{
				Debug.Assert(ob.StockpiledBy == null || ob.StockpiledBy == this);

				if (ob.IsInstalled)
					continue;

				if (Match(ob))
				{
					if (ob.StockpiledBy == null)
						ob.StockpiledBy = this;
				}
				else
				{
					if (ob.StockpiledBy != null)
						ob.StockpiledBy = null;
				}
			}

			if (this.Criteria != null && m_itemTracker.IsEnabled == false)
				EnableItemObjectView();
			else if (this.Criteria == null && m_itemTracker.IsEnabled)
				DisableItemObjectView();
			else
				m_itemTracker.Refresh();
		}

		void EnableItemObjectView()
		{
			m_itemTracker.Enable();

			ItemObject.IsReservedChanged += ItemObject_ParameterChanged;
			ItemObject.IsStockpiledChanged += ItemObject_ParameterChanged;
			ItemObject.IsInstalledChanged += ItemObject_ParameterChanged;
		}

		void DisableItemObjectView()
		{
			m_itemTracker.Disable();

			ItemObject.IsReservedChanged -= ItemObject_ParameterChanged;
			ItemObject.IsStockpiledChanged -= ItemObject_ParameterChanged;
			ItemObject.IsInstalledChanged -= ItemObject_ParameterChanged;
		}

		void ItemObject_ParameterChanged(ItemObject ob)
		{
			m_itemTracker.Update(ob);
		}


		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			if (m_itemTracker.IsEnabled == false)
				return null;

			var ob = m_itemTracker.GetFirst();

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
		public IntPoint3 FindEmptyLocation(out bool ok)
		{
			var env = this.Environment;

			int min = GetMinStack();

			var loc = this.Area.Range().FirstOrDefault(p => GetStack(p) == min);

			if (loc != new IntPoint3())
			{
				ok = true;
				return loc;
			}

			ok = false;
			return new IntPoint3();
		}

		public bool LocationOk(IntPoint3 p, ItemObject ob)
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

		int GetStack(IntPoint3 p)
		{
			return this.Environment.GetContents(p).OfType<ItemObject>().Count();
		}

		bool Match(ItemObject item)
		{
			var c = this.Criteria;

			if (c == null)
				return false;

			return c.Match(item);
		}

		public override string ToString()
		{
			return String.Format("Stockpile");
		}
	}
}
