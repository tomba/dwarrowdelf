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
		protected readonly IEnvironment m_environment;
		[SaveGameProperty]
		DirectionSet m_positioning;
		[SaveGameProperty]
		IntPoint3D m_supposedLocation;
		[SaveGameProperty]
		int m_numFails;
		[SaveGameProperty(Converter = typeof(QueueConverter))]
		Queue<Direction> m_pathDirs;

		class QueueConverter : ISaveGameConverter
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

		public MoveAssignmentBase(IJob parent, ActionPriority priority, IEnvironment environment, DirectionSet positioning)
			: base(parent, priority)
		{
			m_environment = environment;
			m_positioning = positioning;
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

		protected override JobStatus AssignOverride(ILiving worker)
		{
			this.Src = worker.Location;
			m_numFails = 0;

			var res = PreparePath(worker);

			return res;
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

			var action = new MoveAction(dir, this.Priority);
			progress = JobStatus.Ok;
			return action;
		}

		protected override JobStatus ActionProgressOverride(ActionProgressChange e)
		{
			switch (e.State)
			{
				case ActionState.Ok:
					return JobStatus.Ok;

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

		JobStatus PreparePath(ILiving worker)
		{
			var progress = CheckProgress(worker);

			if (progress != Jobs.JobStatus.Ok)
				return progress;

			var path = GetPath(worker);

			if (path == null)
				return Jobs.JobStatus.Abort;

			if (path.Count == 0)
				return Jobs.JobStatus.Done;

			m_pathDirs = path;
			m_supposedLocation = worker.Location;

			return JobStatus.Ok;
		}

		protected abstract Queue<Direction> GetPath(ILiving worker);

		protected abstract JobStatus CheckProgress(ILiving worker);
	}
}
