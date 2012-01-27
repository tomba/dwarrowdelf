using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;
using Dwarrowdelf.Jobs.JobGroups;
using System.Diagnostics;
using Dwarrowdelf.Jobs.AssignmentGroups;

namespace Dwarrowdelf.Client
{
	[SaveGameObjectByRef]
	class ConstructManager : IJobSource, IJobObserver
	{
		[SaveGameProperty]
		EnvironmentObject m_environment;

		[SaveGameProperty]
		List<ConstructJobData> m_jobDataList;

		public ConstructManager(EnvironmentObject ob)
		{
			m_environment = ob;
			m_environment.World.JobManager.AddJobSource(this);

			m_jobDataList = new List<ConstructJobData>();
		}

		ConstructManager(SaveGameContext ctx)
		{
		}

		[OnSaveGamePostDeserialization]
		void OnDeserialized()
		{
			m_environment.World.JobManager.AddJobSource(this);
		}

		public ConstructMode ContainsPoint(IntPoint3 p)
		{
			var data = m_jobDataList.FirstOrDefault(d => d.Location == p);
			if (data == null)
				return ConstructMode.None;
			else
				return data.Mode;
		}

		public void AddConstructJob(ConstructMode mode, IntRectZ area, IItemFilter userItemFilter)
		{
			var locations = area.Range().Where(p => m_environment.Contains(p));

			ITerrainFilter filter;
			IItemFilter coreItemFilter;

			switch (mode)
			{
				case ConstructMode.Floor:
					filter = WorkHelpers.ConstructFloorFilter;
					coreItemFilter = WorkHelpers.ConstructFloorItemFilter;
					break;

				case ConstructMode.Pavement:
					filter = WorkHelpers.ConstructPavementFilter;
					coreItemFilter = WorkHelpers.ConstructPavementItemFilter;
					break;

				case ConstructMode.Wall:
					filter = WorkHelpers.ConstructWallFilter;
					coreItemFilter = WorkHelpers.ConstructWallItemFilter;
					break;

				default:
					throw new Exception();
			}

			IItemFilter itemFilter;

			if (userItemFilter != null)
				itemFilter = new AndItemFilter(coreItemFilter, userItemFilter);
			else
				itemFilter = coreItemFilter;

			locations = locations.Where(p => filter.Match(m_environment.GetTileData(p)));

			foreach (var l in locations)
			{
				var data = new ConstructJobData()
				{
					Mode = mode,
					Location = l,
					ItemFilter = itemFilter,
				};

				m_jobDataList.Add(data);
			}
		}

		public void RemoveArea(IntRectZ area)
		{
			var rm = m_jobDataList.Where(d => area.Contains(d.Location)).ToArray();

			foreach (var d in rm)
			{
				if (d.Job != null)
					d.Job.Abort();

				m_jobDataList.Remove(d);
			}
		}

		#region IJobSource Members

		public IAssignment FindAssignment(ILivingObject living)
		{
			if (m_jobDataList.Count == 0)
				return null;

			foreach (var data in m_jobDataList)
			{
				if (data.Job == null)
				{
					var item = FindItem(living.Location, data.ItemFilter);

					if (item == null)
					{
						Trace.TraceInformation("Failed to find materials");
						continue;
					}

					item.ReservedBy = this;
					data.Item = item;

					var job = new ConstructJob(this, data.Mode, new IItemObject[] { data.Item }, m_environment, data.Location);

					data.Job = job;
					GameData.Data.Jobs.Add(job);
				}

				var assignment = data.Job.FindAssignment(living);

				if (assignment != null)
					return assignment;
			}

			return null;
		}

		#endregion

		#region IJobObserver Members

		public void OnObservableJobStatusChanged(IJob job, JobStatus status)
		{
			var data = m_jobDataList.Single(d => d.Job == job);

			GameData.Data.Jobs.Remove(job);
			data.Job = null;

			Debug.Assert(data.Item.ReservedBy == this);
			data.Item.ReservedBy = null;
			data.Item = null;

			if (status == JobStatus.Done || status == JobStatus.Fail)
				m_jobDataList.Remove(data);
		}

		#endregion

		ItemObject FindItem(IntPoint3 location, IItemFilter filter)
		{
			ItemObject ob = null;

			Func<IntPoint3, bool> func = delegate(IntPoint3 l)
			{
				ob = m_environment.GetContents(l)
					.OfType<ItemObject>()
					.Where(o => o.IsReserved == false && filter.Match(o))
					.FirstOrDefault();

				if (ob != null)
					return true;
				else
					return false;
			};

			var res = AStar.AStarFinder.FindNearest(m_environment, location, func);

			return ob;
		}

		[Serializable]
		class ConstructJobData
		{
			public ConstructMode Mode;
			public IntPoint3 Location;
			public IItemFilter ItemFilter;
			// XXX item criteria
			public ItemObject Item;
			public ConstructJob Job;
		}
	}
}
