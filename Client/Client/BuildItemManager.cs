using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Dwarrowdelf.Jobs;
using System.Collections.Generic;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef(ClientObject = true)]
	sealed class BuildItemManager : IJobSource, IJobObserver, INotifyPropertyChanged
	{
		public ItemObject Workbench { get; private set; }

		public BuildItemInfo BuildItemInfo { get { return Buildings.GetBuildItemInfo(this.Workbench.ItemID); } }
		public EnvironmentObject Environment { get { return this.Workbench.Environment; } }

		[SaveGameProperty]
		ObservableCollection<BuildOrder> m_buildOrderQueue = new ObservableCollection<BuildOrder>();
		public ReadOnlyCollection<BuildOrder> BuildOrderQueue { get; private set; }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.BuildItemManager");

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
			trace.Header = String.Format("BuildItemManager({0})", workbench.ObjectID.Value);

			this.Workbench = workbench;
			this.Workbench.Destructed += OnWorkbenchDestructed;

			this.BuildOrderQueue = new ReadOnlyObservableCollection<BuildOrder>(m_buildOrderQueue);

			m_unreachables = new Unreachables(workbench.World);

			this.Environment.World.JobManager.AddJobSource(this);
		}

		void OnWorkbenchDestructed(BaseObject obj)
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			while (m_buildOrderQueue.Count > 0)
				RemoveBuildOrder(m_buildOrderQueue[0]);

			// XXX remove BuildItemManager from wherever they are stored
		}

		[OnSaveGamePostDeserialization]
		public void OnDeserialized()
		{
			this.Workbench.Destructed += OnWorkbenchDestructed;

			this.BuildOrderQueue = new ReadOnlyObservableCollection<BuildOrder>(m_buildOrderQueue);

			m_unreachables = new Unreachables(this.Workbench.World);

			this.Environment.World.JobManager.AddJobSource(this);

			if (m_currentJob != null)
				GameData.Data.Jobs.Add(m_currentJob);
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

				GameData.Data.Jobs.Remove(job);
				if (job.Status == JobStatus.Ok)
					job.Abort();
			}
		}

		IAssignment IJobSource.FindAssignment(ILivingObject _living)
		{
			var env = this.Environment;
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
				GameData.Data.AddGameEvent(this.Workbench, "Failed to find materials for {0}.", this.CurrentBuildOrder.BuildableItemID);
				return null;
			}

			var job = new Jobs.JobGroups.BuildItemJob(this, this.Workbench, order.BuildableItem.Key, order.SourceItems);
			GameData.Data.Jobs.Add(job);
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

			var numItems = buildableItem.BuildMaterials.Count;

			int numFound = 0;

			for (int i = 0; i < buildableItem.BuildMaterials.Count; ++i)
			{
				var bimi = buildableItem.BuildMaterials[i];
				var biis = order.BuildSpec.ItemSpecs[i];

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
			return String.Format("Building({0:x})", this.Workbench.ObjectID.Value);
		}
	}

	[SaveGameObjectByValue]
	sealed class BuildSpec
	{
		public BuildSpec(BuildableItem buildableItem)
		{
			this.BuildableItem = buildableItem;
			this.BuildableItemFullKey = this.BuildableItem.FullKey;
			this.ItemSpecs = new IItemFilter[this.BuildableItem.BuildMaterials.Count];
		}

		BuildSpec(SaveGameContext context)
		{
			this.BuildableItem = Buildings.FindBuildableItem(this.BuildableItemFullKey);
			Debug.Assert(this.BuildableItem != null);
		}

		[SaveGameProperty]
		string BuildableItemFullKey { get; set; }

		public BuildableItem BuildableItem { get; private set; }

		// extra specs given by the user
		[SaveGameProperty]
		public IItemFilter[] ItemSpecs { get; private set; }
	}

	[SaveGameObjectByRef]
	sealed class BuildOrder : INotifyPropertyChanged
	{
		bool m_isRepeat;
		bool m_isSuspended;
		bool m_isUnderWork;

		public BuildOrder(BuildSpec spec)
		{
			this.BuildSpec = spec;

			if (this.BuildableItem.MaterialID.HasValue)
				this.Name = String.Format("{0} {1}", this.BuildableItem.MaterialID, this.BuildableItem.ItemInfo.Name);
			else
				this.Name = this.BuildableItem.ItemInfo.Name;

			this.SourceItems = new ItemObject[this.BuildableItem.BuildMaterials.Count];
		}

		BuildOrder(SaveGameContext context)
		{
			this.Name = this.BuildableItem.ItemInfo.Name;
		}

		public string Name { get; private set; } // XXX just for UI

		[SaveGameProperty]
		public BuildSpec BuildSpec { get; private set; }

		public BuildableItem BuildableItem { get { return this.BuildSpec.BuildableItem; } }
		public ItemID BuildableItemID { get { return this.BuildSpec.BuildableItem.ItemID; } }

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
	}
}
