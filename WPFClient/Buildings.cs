using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Client
{
	class BuildingData : BaseGameObject
	{
		public BuildingInfo BuildingInfo { get; private set; }
		public Environment Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		class BuildOrder
		{
			public ItemObject[] SourceObjects { get; set; }

			public IJob Job { get; set; }
		}

		List<BuildOrder> m_buildOrderQueue = new List<BuildOrder>();

		public BuildingData(World world, ObjectID objectID, BuildingID id)
			: base(world, objectID)
		{
			this.BuildingInfo = Buildings.GetBuildingInfo(id);
			world.TickIncreased += OnTick;
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
			CheckFinishedOrders();

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
				if (order.Job.Progress == Progress.Done)
				{
					MyDebug.WriteLine("BuildOrder done");
					order.Job = null;
					doneOrders.Add(order);
				}
				else if (order.Job.Progress == Progress.Fail)
				{
					MyDebug.WriteLine("BuildOrder FAILED");
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

		IEnumerable<ItemObject> FindMaterials(BuildOrder order)
		{
			var numItems = order.SourceObjects.Length;

			var items = this.Environment.GetContents().
				OfType<ItemObject>().
				Where(o => o.Assignment == null).
				Take(numItems);

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
				item.Assignment = this;
				order.SourceObjects[i++] = item;
			}
		}

		void CreateJob(BuildOrder order)
		{
			var job = new BuildItemJob(this, order.SourceObjects);
			order.Job = job;
			this.World.JobManager.Add(job);
		}

		void OnTick()
		{
			CheckStatus();
		}
	}
}
