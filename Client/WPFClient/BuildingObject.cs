﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Media;
using System.ComponentModel;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	class BuildingObject : BaseGameObject, IBuildingObject, IDrawableArea
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }
		IEnvironment IBuildingObject.Environment { get { return this.Environment as IEnvironment; } }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		IntCuboid IDrawableArea.Area { get { return new IntCuboid(this.Area, this.Z); } }

		public Brush Fill { get { return null; } }
		public double Opacity { get { return 1.0; } }

		class BuildOrder
		{
			public ItemObject[] SourceObjects { get; set; }

			public Dwarrowdelf.Jobs.IJob Job { get; set; }
		}

		List<BuildOrder> m_buildOrderQueue = new List<BuildOrder>();

		public BuildingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			world.TickStartEvent += OnTick;
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
					building.Z != data.Z ||
					building.Environment != env)
					throw new Exception();

				return;
			}

			this.BuildingInfo = Buildings.GetBuildingInfo(data.ID);
			this.Area = data.Area;
			this.Z = data.Z;
			this.Environment = env;

			env.AddBuilding(this);
		}

		public bool Contains(IntPoint3D point)
		{
			return point.Z == this.Z && this.Area.Contains(point.ToIntPoint());
		}

		public void AddBuildItem()
		{
			var bo = new BuildOrder();
			bo.SourceObjects = new ItemObject[2];

			m_buildOrderQueue.Add(bo);

			CheckStatus();
		}

		void CheckStatus()
		{
			var order = m_buildOrderQueue.FirstOrDefault();
			if (order == null || order.Job != null)
				return;

			var materials = FindMaterials(order);

			if (materials == null)
				return;

			AssignMaterials(order, materials);
			CreateJob(order);
		}

		void CheckFinishedOrders()
		{
			List<BuildOrder> doneOrders = new List<BuildOrder>();

			foreach (var order in m_buildOrderQueue.Where(o => o.Job != null))
			{
				if (order.Job.JobState == Jobs.JobState.Done)
				{
					Debug.Print("BuildOrder done");
					order.Job = null;
					doneOrders.Add(order);
				}
				else if (order.Job.JobState == Jobs.JobState.Fail)
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
		IEnumerable<ItemObject> FindMaterials(BuildOrder order)
		{
			var numItems = order.SourceObjects.Length;

			var items = new ItemObject[numItems];

			for (int i = 0; i < numItems; ++i)
			{
				ItemObject ob = null;

				Func<IntPoint3D, bool> func = delegate(IntPoint3D l)
				{
					ob = this.Environment.GetContents(l).OfType<ItemObject>().Where(o => o.ReservedBy == null && !items.Contains(o)).FirstOrDefault();

					if (ob != null)
						return true;
					else
						return false;
				};

				var res = AStar.AStar3D.FindNearest(new IntPoint3D(this.Area.Center, this.Z), func,
					l => 0,
					this.Environment.GetDirectionsFrom);

				items[i] = ob;
			}

			if (items.Count() != numItems)
				return null;

			return items;
		}

		void AssignMaterials(BuildOrder order, IEnumerable<ItemObject> items)
		{
			Debug.Assert(items.Count() == order.SourceObjects.Length);

			int i = 0;
			foreach (var item in items)
			{
				item.ReservedBy = this;
				order.SourceObjects[i++] = item;
			}
		}

		void CreateJob(BuildOrder order)
		{
			var job = new Jobs.JobGroups.BuildItemJob(this, ActionPriority.Normal, order.SourceObjects);
			job.StateChanged += OnJobStateChanged;
			order.Job = job;
			this.World.JobManager.Add(job);
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			if (state == JobState.Done)
			{
				job.StateChanged -= OnJobStateChanged;
				this.World.JobManager.Remove(job);
				CheckFinishedOrders();
			}
		}

		void OnTick()
		{
			CheckStatus();
		}

		public override string ToString()
		{
			return String.Format("Building({0})", this.ObjectID.Value);
		}
	}
}
