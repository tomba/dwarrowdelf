using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using Dwarrowdelf.Jobs;
using System.Windows;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client
{
	class BuildingObject : BaseGameObject, IBuildingObject, IDrawableElement, IJobSource
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }

		IEnvironment ILargeGameObject.Environment { get { return this.Environment as IEnvironment; } }
		IntCuboid IDrawableElement.Area { get { return new IntCuboid(this.Area); } }

		public IntRectZ Area { get; set; }

		FrameworkElement m_element;
		public FrameworkElement Element { get { return m_element; } }

		public BuildingID BuildingID { get { return this.BuildingInfo.ID; } }

		public string Description { get { return Buildings.GetBuildingInfo(this.BuildingID).Name; } }

		ObservableCollection<BuildOrder> m_buildOrderQueue = new ObservableCollection<BuildOrder>();
		public ReadOnlyCollection<BuildOrder> BuildOrderQueue { get; private set; }

		bool m_initialized;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Building");

		public BuildingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			trace.Header = String.Format("Building({0})", objectID.Value);

			var ellipse = new Rectangle();
			ellipse.Stroke = Brushes.DarkGray;
			ellipse.StrokeThickness = 0.1;
			ellipse.IsHitTestVisible = false;
			m_element = ellipse;

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
			var data = (BuildingData)_data;

			base.Deserialize(_data);

			var env = this.World.FindObject<Environment>(data.Environment);

			this.BuildingInfo = Buildings.GetBuildingInfo(data.ID);
			this.Area = data.Area;
			this.Environment = env;

			m_element.Width = this.Area.Width;
			m_element.Height = this.Area.Height;

			if (m_initialized == false)
			{
				env.AddMapElement(this);

				this.Environment.World.JobManager.AddJobSource(this);

				m_initialized = true;
			}
		}

		public override void Destruct()
		{
			this.Environment.RemoveMapElement(this);

			this.Environment.World.JobManager.RemoveJobSource(this);

			base.Destruct();
		}

		public bool Contains(IntPoint3D point)
		{
			return this.Area.Contains(point);
		}

		BuildingState m_buildingState;
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

		public void AddBuildOrder(BuildableItem buildableItem)
		{
			if (buildableItem == null)
				throw new Exception();

			var bo = new BuildOrder(buildableItem);

			AddBuildOrder(bo);
		}

		IJob m_destructJob;

		CleanAreaJob m_cleanJob;

		BuildOrder m_currentBuildOrder;
		IJob m_currentJob;

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

		void AddBuildOrder(BuildOrder buildOrder)
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
				job.StatusChanged -= OnJobStatusChanged;
				if (job.JobStatus == JobStatus.Ok)
					job.Abort();
			}
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			var env = this.Environment;

			switch (this.BuildingState)
			{
				case Client.BuildingState.NeedsCleaning:
					if (m_cleanJob == null)
					{
						m_cleanJob = new CleanAreaJob(null, this.Environment, this.Area);
						GameData.Data.Jobs.Add(m_cleanJob);
						m_cleanJob.StatusChanged += OnCleanStatusChanged;
					}

					yield return m_cleanJob;

					break;

				case Client.BuildingState.Functional:
					if (this.CurrentBuildOrder == null)
						yield break;

					if (m_currentJob == null)
					{
						var job = CreateJob(this.CurrentBuildOrder);

						if (job == null)
						{
							trace.TraceWarning("XXX failed to create job");
							yield break;
						}

						m_currentJob = job;

						trace.TraceInformation("new build job created");
					}

					yield return m_currentJob;

					break;

				case Client.BuildingState.Destructing:
					if (m_destructJob == null)
					{
						m_destructJob = new Dwarrowdelf.Jobs.AssignmentGroups.MoveDestructBuildingAssignment(null, this);
						GameData.Data.Jobs.Add(m_destructJob);
						m_destructJob.StatusChanged += OnDestructStatusChanged;
					}

					yield return m_destructJob;
					break;

				default:
					throw new Exception();
			}
		}

		void OnDestructStatusChanged(IJob job, JobStatus status)
		{
			// We don't care about the status. If the job failed, it will be retried automatically.

			m_destructJob.StatusChanged -= OnDestructStatusChanged;
			GameData.Data.Jobs.Remove(m_destructJob);
			m_destructJob = null;
		}

		void OnCleanStatusChanged(IJob job, JobStatus status)
		{
			// We don't care about the status. If the job failed, it will be retried automatically.

			m_cleanJob.StatusChanged -= OnCleanStatusChanged;
			GameData.Data.Jobs.Remove(m_cleanJob);
			m_cleanJob = null;
		}

		IJob CreateJob(BuildOrder order)
		{
			var ok = FindMaterials(order);

			if (!ok)
				return null;

			var job = new Jobs.JobGroups.BuildItemJob(this, order.SourceItems, order.BuildableItem.ItemID);
			job.StatusChanged += OnJobStatusChanged;
			GameData.Data.Jobs.Add(job);
			return job;
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

		bool FindMaterials(BuildOrder order)
		{
			var numItems = order.BuildableItem.BuildMaterials.Count;

			int numFound = 0;

			for (int i = 0; i < order.BuildableItem.BuildMaterials.Count; ++i)
			{
				var ob = FindItem(order.BuildableItem.BuildMaterials[i]);

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

		ItemObject FindItem(BuildableItemMaterialInfo itemDef)
		{
			ItemObject ob = null;

			Func<IntPoint3D, bool> func = delegate(IntPoint3D l)
			{
				ob = this.Environment.GetContents(l)
					.OfType<ItemObject>()
					.Where(o => o.ReservedBy == null && itemDef.MatchItem(o))
					.FirstOrDefault();

				if (ob != null)
					return true;
				else
					return false;
			};

			var res = AStar.AStar.FindNearest(this.Environment, this.Area.Center, func);

			return ob;
		}


		public override string ToString()
		{
			return String.Format("Building({0:x})", this.ObjectID.Value);
		}
	}

	class BuildOrder : INotifyPropertyChanged
	{
		bool m_isRepeat;
		bool m_isSuspended;
		bool m_isUnderWork;

		public BuildOrder(BuildableItem buildableItem)
		{
			this.BuildableItem = buildableItem;
			this.SourceItems = new ItemObject[buildableItem.BuildMaterials.Count];
		}

		public BuildableItem BuildableItem { get; private set; }
		public ItemObject[] SourceItems { get; private set; }

		public bool IsRepeat { get { return m_isRepeat; } set { if (m_isRepeat == value) return; m_isRepeat = value; Notify("IsRepeat"); } }
		public bool IsSuspended { get { return m_isSuspended; } set { if (m_isSuspended == value) return; m_isSuspended = value; Notify("IsSuspended"); } }
		public bool IsUnderWork { get { return m_isUnderWork; } set { if (m_isUnderWork == value) return; m_isUnderWork = value; Notify("IsUnderWork"); } }

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}

	[Flags]
	enum BuildingState
	{
		Undefined = 0,
		// XXX cleaning is currently not supported. remove?
		NeedsCleaning = 1 << 0,
		Functional = 1 << 1,
		Destructing = 1 << 2,
		FunctionalNeedsCleaning = Functional | NeedsCleaning,
	}
}
