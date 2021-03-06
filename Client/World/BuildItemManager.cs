﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;
using System.Collections.Generic;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public sealed class BuildItemManager : IJobSource, IJobObserver, INotifyPropertyChanged
	{
		static List<BuildItemManager> s_buildItemManagers;

		static BuildItemManager()
		{
			s_buildItemManagers = new List<BuildItemManager>();
		}

		internal static void AddBuildItemManager(BuildItemManager manager)
		{
			Debug.Assert(s_buildItemManagers.All(bim => bim.Workbench != manager.Workbench));

			s_buildItemManagers.Add(manager);
		}

		static void RemoveBuildItemManager(BuildItemManager mgr)
		{
			var ok = s_buildItemManagers.Remove(mgr);
			Debug.Assert(ok);
		}

		public static BuildItemManager FindBuildItemManager(ItemObject workbench)
		{
			return s_buildItemManagers.FirstOrDefault(m => m.Workbench == workbench);
		}

		public static BuildItemManager FindOrCreateBuildItemManager(ItemObject workbench)
		{
			var mgr = s_buildItemManagers.FirstOrDefault(m => m.Workbench == workbench);

			if (mgr == null)
			{
				mgr = new BuildItemManager(workbench);
				s_buildItemManagers.Add(mgr);
			}

			return mgr;
		}

		[SaveGameProperty]
		public ItemObject Workbench { get; private set; }

		public WorkbenchInfo WorkbenchInfo { get { return Workbenches.GetWorkbenchInfo(this.Workbench.ItemID); } }
		public EnvironmentObject Environment { get { return this.Workbench.Environment; } }

		[SaveGameProperty]
		ObservableCollection<BuildOrder> m_buildOrderQueue;
		public ReadOnlyCollection<BuildOrder> BuildOrderQueue { get; private set; }

		MyTraceSource trace = new MyTraceSource("Client.BuildItemManager");

		[SaveGameProperty]
		BuildOrder m_currentBuildOrder;
		[SaveGameProperty]
		IJobGroup m_currentJob;

		Unreachables m_unreachables;

		// XXX design time
		public BuildItemManager()
		{
		}

		public BuildItemManager(ItemObject workbench)
		{
			this.Workbench = workbench;
			m_buildOrderQueue = new ObservableCollection<BuildOrder>();

			Init();
		}

		BuildItemManager(SaveGameContext ctx)
		{
			Init();
		}

		[OnSaveGamePostDeserialization]
		public void OnDeserialized()
		{
			if (m_currentJob != null)
				this.Environment.World.Jobs.Add(m_currentJob);
		}

		void Init()
		{
			trace.Header = String.Format("BuildItemManager({0})", this.Workbench.ObjectID.Value);

			this.Workbench.Destructed += OnWorkbenchDestructed;
			ItemObject.IsInstalledChanged += OnIsInstalledChanged;

			this.BuildOrderQueue = new ReadOnlyObservableCollection<BuildOrder>(m_buildOrderQueue);

			m_unreachables = new Unreachables(this.Workbench.World);

			this.Environment.World.JobManager.AddJobSource(this);
		}

		void OnWorkbenchDestructed(BaseObject obj)
		{
			Cleanup();
		}

		void OnIsInstalledChanged(ItemObject ob)
		{
			if (ob != this.Workbench)
				return;

			Cleanup();
		}

		void Cleanup()
		{
			this.Workbench.Destructed -= OnWorkbenchDestructed;
			ItemObject.IsInstalledChanged -= OnIsInstalledChanged;

			this.Environment.World.JobManager.RemoveJobSource(this);

			while (m_buildOrderQueue.Count > 0)
				RemoveBuildOrder(m_buildOrderQueue[0]);

			RemoveBuildItemManager(this);
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#endregion

		BuildOrder CurrentBuildOrder
		{
			get { return m_currentBuildOrder; }

			set
			{
				if (m_currentBuildOrder == value)
					return;

				m_currentBuildOrder = value;
				Notify("CurrentBuildOrder");
			}
		}

		BuildOrder FindNextBuildOrder(BuildOrder previousBuildOrder)
		{
			if (m_buildOrderQueue.Count == 0)
				return null;

			int idx;

			if (previousBuildOrder != null)
				idx = m_buildOrderQueue.IndexOf(previousBuildOrder);
			else
				idx = -1;

			for (int i = 0; i < m_buildOrderQueue.Count; ++i)
			{
				idx = (idx + 1) % m_buildOrderQueue.Count;

				var buildOrder = m_buildOrderQueue[idx];

				if (buildOrder.IsSuspended)
					continue;

				return buildOrder;
			}

			return null;
		}

		public void AddBuildOrder(BuildOrder buildOrder)
		{
			buildOrder.PropertyChanged += OnBuildOrderPropertyChanged;
			m_buildOrderQueue.Add(buildOrder);

			trace.TraceInformation("new order {0}", buildOrder);

			if (this.CurrentBuildOrder == null)
				MoveToNextBuildOrder();
		}

		void OnBuildOrderPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (this.CurrentBuildOrder == null || this.CurrentBuildOrder.IsSuspended)
				MoveToNextBuildOrder();
		}

		void RemoveBuildOrder(BuildOrder buildOrder)
		{
			if (this.CurrentBuildOrder != buildOrder)
			{
				buildOrder.PropertyChanged -= OnBuildOrderPropertyChanged;
				var ok = m_buildOrderQueue.Remove(buildOrder);
				Debug.Assert(ok);
			}
			else
			{
				StopCurrentJob();

				buildOrder.IsUnderWork = false;

				var next = FindNextBuildOrder(buildOrder);
				if (next == buildOrder)
					next = null;

				buildOrder.PropertyChanged -= OnBuildOrderPropertyChanged;
				m_buildOrderQueue.Remove(buildOrder);

				this.CurrentBuildOrder = next;

				if (next != null)
					next.IsUnderWork = true;
			}
		}

		void MoveToNextBuildOrder()
		{
			StopCurrentJob();

			var current = this.CurrentBuildOrder;

			if (current != null)
				current.IsUnderWork = false;

			var next = FindNextBuildOrder(current);

			this.CurrentBuildOrder = next;

			if (next != null)
				next.IsUnderWork = true;
		}

		void StopCurrentJob()
		{
			var job = m_currentJob;

			if (job != null)
			{
				m_currentJob = null;

				this.Environment.World.Jobs.Remove(job);
				if (job.Status == JobStatus.Ok)
					job.Abort();
			}
		}

		IAssignment IJobSource.FindAssignment(ILivingObject _living)
		{
			var living = (LivingObject)_living;

			if (this.CurrentBuildOrder == null)
				return null;

			if (m_currentJob == null)
			{
				var job = CreateJob(this.CurrentBuildOrder);

				if (job == null)
				{
					trace.TraceWarning("XXX failed to create job, removing build order");
					RemoveBuildOrder(this.CurrentBuildOrder);
					return null;
				}

				m_currentJob = job;

				trace.TraceInformation("new build job created");
			}

			foreach (var a in m_currentJob.GetAssignments(living))
			{
				if (a.LaborID == LaborID.None || living.GetLaborEnabled(a.LaborID))
					return m_currentJob.FindAssignment(living);
			}

			return null;
		}

		IJobGroup CreateJob(BuildOrder order)
		{
			var ok = FindMaterials(order);

			if (!ok)
			{
				Events.AddGameEvent(this.Workbench, "Failed to find materials for {0}.", this.CurrentBuildOrder.BuildableItemID);
				return null;
			}

			var job = new Jobs.JobGroups.BuildItemJob(this, this.Workbench, order.BuildableItem.Key, order.SourceItems);
			this.Environment.World.Jobs.Add(job);
			return job;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			Debug.Assert(job is Jobs.JobGroups.BuildItemJob);
			OnJobStatusChanged(job, status);
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			StopCurrentJob();

			var sourceItems = this.CurrentBuildOrder.SourceItems;
			for (int i = 0; i < sourceItems.Length; ++i)
			{
				Debug.Assert(sourceItems[i].ReservedBy == this);
				sourceItems[i].ReservedBy = null;
				sourceItems[i] = null;
			}

			switch (status)
			{
				case JobStatus.Done:
					trace.TraceInformation("build job done");

					if (this.CurrentBuildOrder.IsRepeat == false)
						RemoveBuildOrder(this.CurrentBuildOrder);
					else
						MoveToNextBuildOrder();

					break;

				case JobStatus.Abort:
					trace.TraceError("Build item aborted");

					MoveToNextBuildOrder();

					break;

				case JobStatus.Fail:
					trace.TraceError("Build item failed");

					RemoveBuildOrder(this.CurrentBuildOrder);

					break;

				default:
					throw new Exception();
			}
		}

		bool FindMaterials(BuildOrder order)
		{
			var buildableItem = order.BuildableItem;

			var numItems = buildableItem.FixedBuildMaterials.Count;

			int numFound = 0;

			for (int i = 0; i < buildableItem.FixedBuildMaterials.Count; ++i)
			{
				var bimi = buildableItem.FixedBuildMaterials[i];
				var biis = order.UserItemFilters[i];

				var filter = new AndItemFilter(bimi, biis);

				var ob = this.Environment.ItemTracker.GetReachableItemByDistance(this.Workbench.Location, filter, m_unreachables);

				if (ob == null)
					break;

				ob.ReservedBy = this;

				order.SourceItems[i] = ob;
				numFound++;
			}

			if (numFound < numItems)
			{
				trace.TraceInformation("Failed to find materials");
				for (int i = 0; i < numFound; ++i)
				{
					order.SourceItems[i].ReservedBy = null;
					order.SourceItems[i] = null;
				}
				return false;
			}
			else
			{
				return true;
			}
		}

		public override string ToString()
		{
			return String.Format("BuildItemManager({0:x})", this.Workbench.ObjectID.Value);
		}
	}

	[SaveGameObject]
	public sealed class BuildOrder : INotifyPropertyChanged
	{
		bool m_isRepeat;
		bool m_isSuspended;
		bool m_isUnderWork;

		public BuildOrder(BuildableItem buildableItem, IItemFilter[] userFilters)
		{
			this.BuildableItem = buildableItem;
			this.UserItemFilters = userFilters;

			if (this.BuildableItem.MaterialID.HasValue)
				this.Name = String.Format("{0} {1}", this.BuildableItem.MaterialID, this.BuildableItem.ItemInfo.Name);
			else
				this.Name = this.BuildableItem.ItemInfo.Name;

			this.SourceItems = new ItemObject[this.BuildableItem.FixedBuildMaterials.Count];
		}

		BuildOrder(SaveGameContext context)
		{
			Debug.Assert(this.BuildableItem != null);

			if (this.BuildableItem.MaterialID.HasValue)
				this.Name = String.Format("{0} {1}", this.BuildableItem.MaterialID, this.BuildableItem.ItemInfo.Name);
			else
				this.Name = this.BuildableItem.ItemInfo.Name;

		}

		[SaveGameProperty(Converter = typeof(BuildableItemSaveConverter))]
		public BuildableItem BuildableItem { get; private set; }
		public ItemID BuildableItemID { get { return this.BuildableItem.ItemID; } }

		public string Name { get; private set; } // XXX just for UI

		[SaveGameProperty]
		public IItemFilter[] UserItemFilters { get; private set; }

		[SaveGameProperty]
		public ItemObject[] SourceItems { get; private set; }

		[SaveGameProperty]
		public bool IsRepeat { get { return m_isRepeat; } set { if (m_isRepeat == value) return; m_isRepeat = value; Notify("IsRepeat"); } }
		[SaveGameProperty]
		public bool IsSuspended { get { return m_isSuspended; } set { if (m_isSuspended == value) return; m_isSuspended = value; Notify("IsSuspended"); } }
		[SaveGameProperty]
		public bool IsUnderWork { get { return m_isUnderWork; } set { if (m_isUnderWork == value) return; m_isUnderWork = value; Notify("IsUnderWork"); } }

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#endregion

		sealed class BuildableItemSaveConverter : ISaveGameConverter
		{
			public object ConvertToSerializable(object value)
			{
				if (value == null)
					return null;

				var bi = (BuildableItem)value;
				return bi.FullKey;
			}

			public object ConvertFromSerializable(object value)
			{
				if (value == null)
					return null;

				var key = (string)value;
				return Workbenches.FindBuildableItem(key);
			}

			public Type InputType { get { return typeof(BuildableItem); } }

			public Type OutputType { get { return typeof(string); } }
		}
	}
}
