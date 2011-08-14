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

		public BuildingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
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

				this.World.TickStarting += OnTick;
				this.Environment.World.JobManager.AddJobSource(this);

				m_initialized = true;
			}
		}

		public override void Destruct()
		{
			this.Environment.RemoveMapElement(this);

			this.Environment.World.JobManager.RemoveJobSource(this);

			this.World.TickStarting -= OnTick;

			base.Destruct();
		}

		public bool Contains(IntPoint3D point)
		{
			return this.Area.Contains(point);
		}

		public void AddBuildOrder(ItemID itemID)
		{
			var buildableItem = this.BuildingInfo.FindBuildableItem(itemID);

			if (buildableItem == null)
				throw new Exception();

			var bo = new BuildOrder(buildableItem);

			m_buildOrderQueue.Add(bo);

			CheckStatus();
		}

		public void AddBuildOrder(BuildableItem buildableItem)
		{
			if (buildableItem == null)
				throw new Exception();

			var bo = new BuildOrder(buildableItem);

			m_buildOrderQueue.Add(bo);

			CheckStatus();
		}

		void CheckStatus()
		{
			var order = m_buildOrderQueue.FirstOrDefault();
			if (order == null || order.Job != null)
				return;

			var ok = FindMaterials(order);

			if (!ok)
				return;

			CreateJob(order);
		}

		void CheckFinishedOrders()
		{
			List<BuildOrder> doneOrders = new List<BuildOrder>();

			foreach (var order in m_buildOrderQueue.Where(o => o.Job != null))
			{
				if (order.Job.JobStatus == Jobs.JobStatus.Done)
				{
					Debug.Print("BuildOrder done");
					order.Job = null;
					doneOrders.Add(order);
				}
				else if (order.Job.JobStatus == Jobs.JobStatus.Fail)
				{
					Debug.Print("BuildOrder FAILED");
					order.Job = null;
					doneOrders.Add(order);
				}
				else
				{
					// not started or in progress
				}
			}

			foreach (var order in doneOrders)
				m_buildOrderQueue.Remove(order);
		}

		/* find the materials closest to this building.
		 * XXX path should be saved, or path should be determined later */
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
				Trace.TraceInformation("Failed to find materials");
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

		bool IJobSource.HasWork
		{
			get
			{
				if (this.BuildingState == Dwarrowdelf.BuildingState.NeedsCleaning)
					return true;

				return m_buildOrderQueue.Where(bo => bo.Job != null).Count() > 0;
			}
		}

		CleanAreaJob m_cleanJob;

		IEnumerable<IJob> IJobSource.GetJobs(ILiving living)
		{
			var env = this.Environment;

			if (this.BuildingState == Dwarrowdelf.BuildingState.NeedsCleaning)
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
				var order = m_buildOrderQueue.Where(bo => bo.Job != null).FirstOrDefault();
				if (order != null)
					yield return order.Job;
			}
		}

		void OnCleanStatusChanged(IJob job, JobStatus status)
		{
			if (status != JobStatus.Done)
				throw new Exception();

			m_cleanJob.StatusChanged -= OnCleanStatusChanged;
			GameData.Data.Jobs.Remove(m_cleanJob);
			m_cleanJob = null;
		}

		void CreateJob(BuildOrder order)
		{
			var job = new Jobs.JobGroups.BuildItemJob(this, ActionPriority.Normal,
				order.SourceItems, order.BuildableItem.ItemID);
			job.StatusChanged += OnJobStatusChanged;
			order.Job = job;
			GameData.Data.Jobs.Add(job);
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			if (status == JobStatus.Fail || status == JobStatus.Abort)
				Trace.WriteLine("Build item failed");

			GameData.Data.Jobs.Remove(job);
			job.StatusChanged -= OnJobStatusChanged;
			CheckFinishedOrders();
		}

		void OnTick()
		{
			CheckStatus();
		}

		public override string ToString()
		{
			return String.Format("Building({0:x})", this.ObjectID.Value);
		}

		public class BuildOrder
		{
			public BuildOrder(BuildableItem buildableItem)
			{
				this.BuildableItem = buildableItem;
				this.SourceItems = new ItemObject[buildableItem.BuildMaterials.Count];
			}

			public BuildableItem BuildableItem { get; private set; }
			public ItemObject[] SourceItems { get; private set; }

			public Dwarrowdelf.Jobs.IJob Job { get; set; }

			public bool IsRepeat { get; set; }
			public bool IsSuspended { get; set; }
		}
	}
}
