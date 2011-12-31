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
	sealed class BuildingObject : BaseObject, IBuildingObject, IDrawableElement, IJobSource, IJobObserver
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public EnvironmentObject Environment { get; set; }

		IEnvironmentObject IAreaObject.Environment { get { return this.Environment as IEnvironmentObject; } }
		IntCuboid IDrawableElement.Area { get { return new IntCuboid(this.Area); } }

		public IntRectZ Area { get; set; }

		public BuildingID BuildingID { get { return this.BuildingInfo.BuildingID; } }

		public string Description { get { return Buildings.GetBuildingInfo(this.BuildingID).Name; } }

		[SaveGameProperty]
		ObservableCollection<BuildOrder> m_buildOrderQueue = new ObservableCollection<BuildOrder>();
		public ReadOnlyCollection<BuildOrder> BuildOrderQueue { get; private set; }

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Building");

		[SaveGameProperty]
		IAssignment m_destructJob;

		[SaveGameProperty]
		BuildOrder m_currentBuildOrder;
		[SaveGameProperty]
		IJobGroup m_currentJob;

		[SaveGameProperty]
		BuildingState m_buildingState;

		/// <summary>
		/// For Design-time only
		/// </summary>
		public BuildingObject()
			: this(null, ObjectID.NullObjectID)
		{
			var r = new Random();

			var props = new Tuple<PropertyID, object>[]
			{
			};

			var data = new BuildingData()
			{
				ObjectID = new ObjectID(ObjectType.Living, (uint)r.Next(5000)),
				CreationTick = r.Next(),
				CreationTime = DateTime.Now,
				Properties = props,

				BuildingID = Dwarrowdelf.BuildingID.Smelter,
				Area = new IntRectZ(new IntRect(0, 0, 4, 4), 9),
			};

			Deserialize(data);
		}

		public BuildingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			trace.Header = String.Format("Building({0})", objectID.Value);

			this.BuildOrderQueue = new ReadOnlyObservableCollection<BuildOrder>(m_buildOrderQueue);

			this.BuildingState = Client.BuildingState.Functional;
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				default:
					throw new Exception();
			}
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var initialized = this.IsInitialized;

			var data = (BuildingData)_data;

			base.Deserialize(_data);

			this.BuildingInfo = Buildings.GetBuildingInfo(data.BuildingID);
			this.Area = data.Area;

			// no env at design time
			if (data.Environment != ObjectID.NullObjectID)
			{
				var env = this.World.GetObject<EnvironmentObject>(data.Environment);

				this.Environment = env;

				if (initialized == false)
				{
					env.AddMapElement(this);

					this.Environment.World.JobManager.AddJobSource(this);
				}
			}
		}

		public override void Destruct()
		{
			this.Environment.RemoveMapElement(this);

			this.Environment.World.JobManager.RemoveJobSource(this);

			base.Destruct();
		}

		[OnSaveGamePostDeserialization]
		public void OnDeserialized()
		{
			this.BuildOrderQueue = new ReadOnlyObservableCollection<BuildOrder>(m_buildOrderQueue);

			if (m_currentJob != null)
				GameData.Data.Jobs.Add(m_currentJob);

			if (m_destructJob != null)
				GameData.Data.Jobs.Add(m_destructJob);
		}

		public bool Contains(IntPoint3D point)
		{
			return this.Area.Contains(point);
		}

		public BuildingState BuildingState
		{
			get { return m_buildingState; }
			set
			{
				m_buildingState = value;
				Notify("BuildingState");
			}
		}

		public void DestructBuilding()
		{
			this.BuildingState = Client.BuildingState.Destructing;

			while (m_buildOrderQueue.Count > 0)
				RemoveBuildOrder(m_buildOrderQueue[0]);
		}

		public void CancelDestructBuilding()
		{
			this.BuildingState = Client.BuildingState.Functional;

			if (m_destructJob != null)
			{
				m_destructJob.Abort();
				m_destructJob = null;
			}
		}

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

		IAssignment IJobSource.FindAssignment(ILivingObject living)
		{
			var env = this.Environment;

			switch (this.BuildingState)
			{
				case Client.BuildingState.Functional:
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

					return m_currentJob.FindAssignment(living);

				case Client.BuildingState.Destructing:
					if (m_destructJob != null)
						return null;

					m_destructJob = new Dwarrowdelf.Jobs.AssignmentGroups.MoveDestructBuildingAssignment(null, this);
					GameData.Data.Jobs.Add(m_destructJob);
					return m_destructJob;

				default:
					throw new Exception();
			}
		}

		IJobGroup CreateJob(BuildOrder order)
		{
			var ok = FindMaterials(order);

			if (!ok)
				return null;

			var job = new Jobs.JobGroups.BuildItemJob(this, this, order.BuildableItem.Key, order.SourceItems);
			GameData.Data.Jobs.Add(job);
			return job;
		}

		void IJobObserver.OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			switch (this.BuildingState)
			{
				case Client.BuildingState.Functional:
					Debug.Assert(job is Jobs.JobGroups.BuildItemJob);
					OnJobStatusChanged(job, status);
					break;

				case Client.BuildingState.Destructing:
					Debug.Assert(job is Dwarrowdelf.Jobs.AssignmentGroups.MoveDestructBuildingAssignment);
					OnDestructStatusChanged(job, status);
					break;

				default:
					throw new Exception();
			}
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			bool fail = false;

			switch (status)
			{
				case JobStatus.Done:
					trace.TraceInformation("build job done");
					break;

				case JobStatus.Abort:
					trace.TraceError("Build item aborted");
					break;

				case JobStatus.Fail:
					fail = true;
					trace.TraceError("Build item failed");
					break;

				default:
					throw new Exception();
			}

			StopCurrentJob();

			if (fail || this.CurrentBuildOrder.IsRepeat == false)
				RemoveBuildOrder(this.CurrentBuildOrder);
			else
				MoveToNextBuildOrder();
		}

		void OnDestructStatusChanged(IJob job, JobStatus status)
		{
			// We don't care about the status. If the job failed, it will be retried automatically.

			GameData.Data.Jobs.Remove(m_destructJob);
			m_destructJob = null;
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

				var ob = FindItem(o => bimi.MatchItem(o) && biis.MatchItem(o));

				if (ob == null)
					break;

				order.SourceItems[i] = ob;
				numFound++;
			}

			if (numFound < numItems)
			{
				trace.TraceInformation("Failed to find materials");
				for (int i = 0; i < numFound; ++i)
					order.SourceItems[i] = null;
				return false;
			}
			else
			{
				return true;
			}
		}

		ItemObject FindItem(Func<ItemObject, bool> match)
		{
			ItemObject ob = null;

			Func<IntPoint3D, bool> func = delegate(IntPoint3D l)
			{
				ob = this.Environment.GetContents(l)
					.OfType<ItemObject>()
					.Where(o => o.ReservedBy == null && match(o))
					.FirstOrDefault();

				if (ob != null)
					return true;
				else
					return false;
			};

			var res = AStar.AStarFinder.FindNearest(this.Environment, this.Area.Center, func);

			return ob;
		}

		public override string ToString()
		{
			return String.Format("Building({0:x})", this.ObjectID.Value);
		}
	}

	[Serializable]
	sealed class SourceItemSpec
	{
		public ItemID[] ItemIDs { get; set; }
		public MaterialID[] MaterialIDs { get; set; }

		public bool MatchItem(IItemObject ob)
		{
			return (this.ItemIDs.Length == 0 || this.ItemIDs.Contains(ob.ItemID)) &&
				(this.MaterialIDs.Length == 0 || this.MaterialIDs.Contains(ob.MaterialID));
		}
	}

	[SaveGameObjectByValue]
	sealed class BuildSpec
	{
		public BuildSpec(BuildableItem buildableItem)
		{
			this.BuildableItem = buildableItem;
			this.BuildableItemKey = this.BuildableItem.Key;
			this.ItemSpecs = new SourceItemSpec[this.BuildableItem.BuildMaterials.Count];
		}

		BuildSpec(SaveGameContext context)
		{
			// XXX BuildableItem is not restored properly. Needs to know the building ID...
		}

		[SaveGameProperty]
		string BuildableItemKey { get; set; }

		public BuildableItem BuildableItem { get; private set; }

		// extra specs given by the user
		[SaveGameProperty]
		public SourceItemSpec[] ItemSpecs { get; private set; }
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

	[Flags]
	enum BuildingState
	{
		Undefined = 0,
		Functional = 1 << 1,
		Destructing = 1 << 2,
	}
}
