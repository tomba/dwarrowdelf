﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public sealed class Stockpile : IAreaElement, IJobSource, IJobObserver
	{
		public static Stockpile CreateStockpile(EnvironmentObject environment, IntGrid2Z area)
		{
			var stockpile = new Stockpile(environment, area);
			environment.AddAreaElement(stockpile);
			return stockpile;
		}

		public static void DestructStockpile(Stockpile stockpile)
		{
			stockpile.Environment.RemoveAreaElement(stockpile);
		}

		[SaveGameProperty]
		public EnvironmentObject Environment { get; private set; }

		[SaveGameProperty]
		public IntGrid2Z Area { get; private set; }

		// XXX Just one criteria for now. Could be multiple in the future.
		[SaveGameProperty]
		public ItemFilter Criteria { get; private set; }

		[SaveGameProperty]
		List<StoreToStockpileJob> m_jobs;

		public string Description { get { return "Stockpile"; } }
		public SymbolID SymbolID { get { return Client.SymbolID.Contraption; } }
		public GameColor EffectiveColor { get { return GameColor.Gray; } }

		TargetItemTracker m_itemTracker;

		public Stockpile(EnvironmentObject environment, IntGrid2Z area)
		{
			this.Environment = environment;
			this.Area = area;

			m_jobs = new List<StoreToStockpileJob>();
		}

		Stockpile(SaveGameContext ctx)
		{
		}

		public void Register()
		{
			this.Environment.World.JobManager.AddJobSource(this);

			this.Environment.ObjectRemoved += Environment_ObjectRemoved;
			this.Environment.ObjectMoved += Environment_ObjectMoved;

			m_itemTracker = new TargetItemTracker(this.Environment, this.Area.Center,
				o => o.IsReserved == false && o.IsStockpiled == false && o.IsInstalled == false && Match(o));

			foreach (var job in m_jobs)
				this.Environment.World.Jobs.Add(job);
		}

		public void Unregister()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			this.Environment.ObjectRemoved -= Environment_ObjectRemoved;
			this.Environment.ObjectMoved -= Environment_ObjectMoved;

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

		void Environment_ObjectMoved(MovableObject ob, IntVector3 oldPos)
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

		public void SetCriteria(ItemFilter itemFilter)
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

			this.Environment.World.Jobs.Add(job);

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
					Events.AddGameEvent(j.Item, "failed to store item to stockpile");
					break;

				default:
					throw new Exception();
			}

			Debug.Assert(j.Item.ReservedBy == this);
			j.Item.ReservedBy = null;

			m_jobs.Remove(j);

			this.Environment.World.Jobs.Remove(j);
		}

		// XXX Silly algorithm. Fill the stockpile evenly.
		public IntVector3 FindEmptyLocation(out bool ok)
		{
			int min = GetMinStack();

			var loc = this.Area.Range().FirstOrDefault(p => GetStack(p) == min);

			if (loc != new IntVector3())
			{
				ok = true;
				return loc;
			}

			ok = false;
			return new IntVector3();
		}

		public bool LocationOk(IntVector3 p, ItemObject ob)
		{
			if (!this.Area.Contains(p))
				throw new Exception();

			return GetStack(p) == GetMinStack();
		}

		int GetMinStack()
		{
			return this.Area.Range().Min(p => GetStack(p));
		}

		int GetStack(IntVector3 p)
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
