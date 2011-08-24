using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Jobs;

namespace Dwarrowdelf.Client
{
	class ConstructionSite : IJobSource, IDrawableElement
	{
		public BuildingID BuildingID;
		public Environment Environment { get; private set; }
		public IntRectZ Area { get; private set; }
		IntCuboid IDrawableElement.Area { get { return new IntCuboid(this.Area); } }
		public System.Windows.FrameworkElement Element { get; private set; }

		public string Description { get { return "Construction (" + Buildings.GetBuildingInfo(this.BuildingID).Name + ")"; } }

		public ConstructionSite(Environment environment, BuildingID buildingID, IntRectZ area)
		{
			this.Environment = environment;
			this.BuildingID = buildingID;
			this.Area = area;

			var rect = new System.Windows.Shapes.Rectangle();
			rect.Stroke = System.Windows.Media.Brushes.Cyan;
			rect.StrokeThickness = 0.1;
			rect.Width = this.Area.Width;
			rect.Height = this.Area.Height;
			rect.IsHitTestVisible = false;
			this.Element = rect;

			this.Environment.World.JobManager.AddJobSource(this);
		}

		public void Destruct()
		{
			this.Environment.World.JobManager.RemoveJobSource(this);
		}

		ConstructBuildingJob m_job;

		IAssignment IJobSource.FindAssignment(ILiving living)
		{
			if (m_job == null)
			{
				m_job = new ConstructBuildingJob(null, this.Environment, this.Area, this.BuildingID);
				GameData.Data.Jobs.Add(m_job);
				m_job.StatusChanged += OnJobStatusChanged;
			}

			return m_job.FindAssignment(living);
		}

		void OnJobStatusChanged(IJob job, JobStatus status)
		{
			if (status != JobStatus.Done)
				throw new Exception();

			m_job.StatusChanged -= OnJobStatusChanged;
			GameData.Data.Jobs.Remove(m_job);
			m_job = null;

			this.Environment.RemoveMapElement(this);
			this.Destruct();
		}
	}
}
