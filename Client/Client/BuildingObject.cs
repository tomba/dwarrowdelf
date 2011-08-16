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
		}

		BuildingState m_state;
		public BuildingState BuildingState
		{
			get { return m_state; }
			private set { m_state = value; Notify("BuildingState"); }
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.BuildingState:
					this.BuildingState = (BuildingState)value;
					break;

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

		bool m_destructingBuilding;
		public void DestructBuilding()
		{
			m_destructingBuilding = true;
		}

		public void AddBuildOrder(BuildableItem buildableItem)
		{
			if (buildableItem == null)
				throw new Exception();

			var bo = new BuildOrder(buildableItem);

			m_buildOrderQueue.Add(bo);

			if (this.CurrentBuildOrder == null)
			{
				bo.IsUnderWork = true;
				this.CurrentBuildOrder = bo;
				trace.TraceInformation("new order {0}", this.CurrentBuildOrder);
			}
		}

		bool IJobSource.HasWork
		{
			get
			{
				if (this.BuildingState == Dwarrowdelf.BuildingState.NeedsCleaning)
					return true;

				if (m_destructingBuilding)
					return true;

				return this.CurrentBuildOrder != null;
			}
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

		void RemoveBuildOrder(BuildOrder buildOrder)
		{
			if (this.CurrentBuildOrder != buildOrder)
			{
				var ok = m_buildOrderQueue.Remove(buildOrder);
				Debug.Assert(ok);
			}
			else
			{
				if (m_currentJob != null)
				{
					GameData.Data.Jobs.Remove(m_currentJob);
					m_currentJob.StatusChanged -= OnJobStatusChanged;
					m_currentJob.Abort();
					m_currentJob = null;
				}

				var next = FindNextBuildOrder(buildOrder);
				if (next == buildOrder)
					next = null;

				m_buildOrderQueue.Remove(buildOrder);

				if (next != null)
					next.IsUnderWork = true;

				this.CurrentBuildOrder = next;
			}
		}

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			var env = this.Environment;

			if (m_destructingBuilding)
			{
				if (m_destructJob == null)
				{
					m_destructJob = new Dwarrowdelf.Jobs.AssignmentGroups.MoveDestructBuildingAssignment(null, ActionPriority.Normal, this);
					GameData.Data.Jobs.Add(m_destructJob);
					m_destructJob.StatusChanged += OnDestructStatusChanged;
				}

				yield return m_destructJob;
			}
			else if (this.BuildingState == Dwarrowdelf.BuildingState.NeedsCleaning)
			{
				if (m_cleanJob == null)
				{
					m_cleanJob = new CleanAreaJob(null, ActionPriority.Normal, this.Environment, this.Area);
					GameData.Data.Jobs.Add(m_cleanJob);
					m_cleanJob.StatusChanged += OnCleanStatusChanged;
				}

				yield return m_cleanJob;
			}
			else
			{
				if (this.CurrentBuildOrder == null)
				{
					trace.TraceInformation("XXX current order null");
					yield break;
				}

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
			}
		}

		void OnDestructStatusChanged(IJob job, JobStatus status)
		{
			if (status != JobStatus.Done)
				throw new Exception();

			m_destructJob.StatusChanged -= OnDestructStatusChanged;
			GameData.Data.Jobs.Remove(m_destructJob);
			m_destructJob = null;
		}

		void OnCleanStatusChanged(IJob job, JobStatus status)
		{
			if (status != JobStatus.Done)
				throw new Exception();

			m_cleanJob.StatusChanged -= OnCleanStatusChanged;
			GameData.Data.Jobs.Remove(m_cleanJob);
			m_cleanJob = null;
		}

		IJob CreateJob(BuildOrder order)
		{
			var ok = FindMaterials(order);

			if (!ok)
				return null;

			var job = new Jobs.JobGroups.BuildItemJob(this, ActionPriority.Normal, order.SourceItems, order.BuildableItem.ItemID);
			job.StatusChanged += OnJobStatusChanged;
			GameData.Data.Jobs.Add(job);
			return job;
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			switch (status)
			{
				case JobStatus.Done:
					trace.TraceInformation("build job done");
					break;

				case JobStatus.Abort:
				case JobStatus.Fail:
					trace.TraceError("Build item failed");
					break;

				default:
					throw new Exception();
			}

			m_currentJob = null;
			GameData.Data.Jobs.Remove(job);
			job.StatusChanged -= OnJobStatusChanged;

			var old = this.CurrentBuildOrder;
			var next = FindNextBuildOrder(old);

			old.IsUnderWork = false;
			next.IsUnderWork = true;

			this.CurrentBuildOrder = next;

			if (old.IsRepeat == false)
				RemoveBuildOrder(old);
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

		public bool IsRepeat { get { return m_isRepeat; } set { m_isRepeat = value; Notify("IsRepeat"); } }
		public bool IsSuspended { get { return m_isSuspended; } set { m_isSuspended = value; Notify("IsSuspended"); } }
		public bool IsUnderWork { get { return m_isUnderWork; } set { m_isUnderWork = value; Notify("IsUnderWork"); } }

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		protected void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}
}
