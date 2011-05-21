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

namespace Dwarrowdelf.Client
{
	class BuildingObject : BaseGameObject, IBuildingObject, IDrawableElement, IJobSource
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }

		IEnvironment IBuildingObject.Environment { get { return this.Environment as IEnvironment; } }
		IntCuboid IDrawableElement.Area { get { return new IntCuboid(this.Area); } }

		public IntRect3D Area { get; set; }

		FrameworkElement m_element;
		public FrameworkElement Element { get { return m_element; } }

		class BuildOrder
		{
			public BuildOrder(BuildableItem buildableItem)
			{
				this.BuildableItem = buildableItem;
				this.SourceItems = new ItemObject[buildableItem.BuildMaterials.Count];
			}

			public BuildableItem BuildableItem { get; private set; }
			public ItemObject[] SourceItems { get; private set; }

			public Dwarrowdelf.Jobs.IJob Job { get; set; }
		}

		List<BuildOrder> m_buildOrderQueue = new List<BuildOrder>();

		public BuildingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			var ellipse = new Rectangle();
			ellipse.Stroke = Brushes.DarkGray;
			ellipse.StrokeThickness = 0.1;
			m_element = ellipse;
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (BuildingData)_data;

			var env = this.World.FindObject<Environment>(data.Environment);

			if (env.Buildings.Contains(this.ObjectID))
			{
				/* this shouldn't happen, as building's data are currently never modified.
				 * however, we get this from object creation also. for now, just check if the
				 * data are the same, and go on. */
				var building = env.Buildings[data.ObjectID];

				if (building.Area != data.Area ||
					building.Environment != env)
					throw new Exception();

				return;
			}

			this.BuildingInfo = Buildings.GetBuildingInfo(data.ID);
			this.Area = data.Area;
			this.Environment = env;

			m_element.Width = this.Area.Width;
			m_element.Height = this.Area.Height;

			env.AddBuilding(this);

			this.World.TickStartEvent += OnTick;
			this.Environment.World.JobManager.AddJobSource(this);
		}

		public override void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);

			this.World.TickStartEvent -= OnTick;

			base.Destruct();
		}

		public bool Contains(IntPoint3D point)
		{
			return this.Area.Contains(point);
		}

		public void AddBuildOrder(ItemID itemID, MaterialID materialID)
		{
			throw new NotImplementedException();
		}

		public void AddBuildOrder(ItemID itemID, MaterialClass materialClass)
		{
			var buildableItem = this.BuildingInfo.FindBuildableItem(itemID);

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
				return m_buildOrderQueue.Where(bo => bo.Job != null).Count() > 0;
			}
		}

		IAssignment IJobSource.GetJob(ILiving living)
		{
			var order = m_buildOrderQueue.Where(bo => bo.Job != null).FirstOrDefault();
			if (order != null)
			{
				var job = order.Job;

				var assignment = JobManager.FindAssignment(job, living);

				if (assignment == null)
					return null;

				var jobState = assignment.Assign(living);

				switch (jobState)
				{
					case JobStatus.Ok:
						return assignment;

					case JobStatus.Done:
						throw new Exception();

					case JobStatus.Abort:
					case JobStatus.Fail:
						break;

					default:
						throw new Exception();
				}
			}

			return null;
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
			if (status == JobStatus.Done)
			{
				GameData.Data.Jobs.Remove(job);
				job.StatusChanged -= OnJobStatusChanged;
				CheckFinishedOrders();
			}
		}

		void OnTick()
		{
			CheckStatus();
		}

		public override string ToString()
		{
			return String.Format("Building({0:x})", this.ObjectID.Value);
		}
	}
}
