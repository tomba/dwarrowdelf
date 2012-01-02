using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Jobs.Assignments
{
	public abstract class MoveAssignmentBase : Assignment
	{
		[SaveGameProperty]
		protected IntPoint3D Src { get; private set; } // just for ToString()

		[SaveGameProperty]
		protected readonly IEnvironmentObject m_environment;
		[SaveGameProperty]
		DirectionSet m_positioning;
		[SaveGameProperty]
		IntPoint3D m_supposedLocation;
		[SaveGameProperty]
		int m_numFails;
		[SaveGameProperty(Converter = typeof(QueueConverter))]
		Queue<Direction> m_pathDirs;
		[SaveGameProperty]
		public IItemObject HauledItem { get; private set; }

		sealed class QueueConverter : ISaveGameConverter
		{
			public object ConvertToSerializable(object value)
			{
				if (value == null)
					return null;

				var q = (Queue<Direction>)value;
				return q.ToArray();
			}

			public object ConvertFromSerializable(object value)
			{
				if (value == null)
					return null;

				var a = (Direction[])value;
				return new Queue<Direction>(a);
			}

			public Type InputType { get { return typeof(Queue<Direction>); } }

			public Type OutputType { get { return typeof(Direction[]); } }
		}

		protected MoveAssignmentBase(IJobObserver parent, IEnvironmentObject environment, DirectionSet positioning)
			: base(parent)
		{
			m_environment = environment;
			m_positioning = positioning;
		}

		protected MoveAssignmentBase(IJobObserver parent, IEnvironmentObject environment, DirectionSet positioning, IItemObject hauledItem)
			: this(parent, environment, positioning)
		{
			this.HauledItem = hauledItem;
		}

		protected MoveAssignmentBase(SaveGameContext ctx)
			: base(ctx)
		{
		}

		public DirectionSet Positioning
		{
			get { return m_positioning; }
			set
			{
				m_positioning = value;
				m_pathDirs = null;
			}
		}

		protected override void OnStateChanged(JobStatus status)
		{
			if (status == JobStatus.Ok)
				return;

			// else Abort, Done or Fail
			m_pathDirs = null;
			m_numFails = 0;
		}

		protected override void AssignOverride(ILivingObject worker)
		{
			this.Src = worker.Location;
			m_numFails = 0;
		}

		protected override GameAction PrepareNextActionOverride(out JobStatus progress)
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var res = PreparePath(this.Worker);

				if (res != JobStatus.Ok)
				{
					progress = res;
					return null;
				}
			}

			Direction dir = m_pathDirs.Dequeue();

			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			m_supposedLocation += new IntVector3D(dir);

			GameAction action;

			if (this.HauledItem == null)
				action = new MoveAction(dir);
			else
				action = new HaulAction(dir, this.HauledItem);

			progress = JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionDoneOverride(ActionState actionStatus)
		{
			switch (actionStatus)
			{
				case ActionState.Done:
					return CheckProgress(this.Worker);

				case ActionState.Fail:
					m_numFails++;
					if (m_numFails > 10)
						return JobStatus.Abort;

					var res = PreparePath(this.Worker);
					return res;

				case ActionState.Abort:
					return JobStatus.Abort;

				default:
					throw new Exception();
			}
		}

		JobStatus PreparePath(ILivingObject worker)
		{
			var progress = CheckProgress(worker);

			if (progress != JobStatus.Ok)
				return progress;

			var path = GetPath(worker);

			if (path == null)
				return JobStatus.Abort;

			if (path.Count == 0)
				return JobStatus.Done;

			m_pathDirs = path;
			m_supposedLocation = worker.Location;

			return JobStatus.Ok;
		}

		protected abstract Queue<Direction> GetPath(ILivingObject worker);

		protected abstract JobStatus CheckProgress(ILivingObject worker);
	}
}
