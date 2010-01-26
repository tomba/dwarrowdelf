using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyGame.Client
{
	abstract class ActionJob : IActionJob, INotifyPropertyChanged
	{
		protected ActionJob(IJob parent)
		{
			this.Parent = parent;
		}

		public IJob Parent { get; private set; }

		Living m_worker;
		public Living Worker
		{
			get { return m_worker; }
			protected set { m_worker = value; Notify("Worker"); }
		}

		GameAction m_action;
		public virtual GameAction CurrentAction
		{
			get { return m_action; }
			protected set { m_action = value; Notify("CurrentAction"); }
		}

		Progress m_progress;
		public Progress Progress
		{
			get { return m_progress; }
			private set { m_progress = value; Notify("Progress"); }
		}



		public Progress Assign(Living worker)
		{
			Debug.Assert(this.Worker == null);
			Debug.Assert(this.Progress == Progress.None);

			var progress = AssignOverride(worker);
			SetProgress(progress);
			if (progress != Progress.Ok)
				return progress;

			this.Worker = worker;

			return progress;
		}

		protected virtual Progress AssignOverride(Living worker)
		{
			return Progress.Ok;
		}



		public Progress PrepareNextAction()
		{
			var progress = PrepareNextActionOverride();
			SetProgress(progress);
			return progress;
		}

		protected abstract Progress PrepareNextActionOverride();

		public Progress ActionProgress(ActionProgressEvent e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Progress == Progress.Ok);
			Debug.Assert(this.CurrentAction != null);
			Debug.Assert(e.TransactionID == this.CurrentAction.TransactionID);

			var progress = ActionProgressOverride(e);
			SetProgress(progress);
			Notify("CurrentAction");
			return progress;
		}
		
		protected virtual Progress ActionProgressOverride(ActionProgressEvent e)
		{
			return Progress.Ok;
		}


		protected void SetProgress(Progress progress)
		{
			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					break;

				case Progress.Done:
					Cleanup();
					this.Worker = null;
					this.CurrentAction = null;
					break;

				case Progress.Abort:
					this.Worker = null;
					this.CurrentAction = null;
					break;

				case Progress.Fail:
					Cleanup();
					this.Worker = null;
					this.CurrentAction = null;
					break;
			}

			this.Progress = progress;
		}

		protected virtual void Cleanup()
		{
		}


		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	class MoveActionJob : ActionJob
	{
		Queue<Direction> m_pathDirs;
		Environment m_environment;
		IntPoint3D m_dest;
		bool m_adjacent;
		IntPoint3D m_supposedLocation;
		int m_numFails;

		public MoveActionJob(IJob parent, Environment environment, IntPoint3D destination, bool adjacent)
			: base(parent)
		{
			m_environment = environment;
			m_dest = destination;
			m_adjacent = adjacent;
		}

		protected override void Cleanup()
		{
			m_environment = null;
			m_pathDirs = null;
		}

		protected override Progress PrepareNextActionOverride()
		{
			if (m_pathDirs == null || m_supposedLocation != this.Worker.Location)
			{
				var res = PreparePath();

				if (res != Progress.Ok)
					return res;
			}

			Direction dir = m_pathDirs.Dequeue();

			if (m_pathDirs.Count == 0)
				m_pathDirs = null;

			var action = new MoveAction(dir);
			m_supposedLocation += IntVector3D.FromDirection(dir);

			this.CurrentAction = action;

			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressEvent e)
		{
			if (e.Success == false)
			{
				m_numFails++;
				if (m_numFails > 10)
					return Progress.Fail;

				var res = PreparePath();
				return res;
			}

			if (e.TicksLeft > 0)
				return Progress.Ok;

			return CheckProgress();
		}

		Progress PreparePath()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
			{
				m_pathDirs = null;
				return Progress.Done;
			}

			// ZZZ only 2D
			int z = m_dest.Z;
			var src2d = this.Worker.Location2D;
			var dest2d = new IntPoint(m_dest.X, m_dest.Y);
			var env = m_environment;
			var res = AStar.Find(src2d, dest2d, !m_adjacent,
				l => env.IsWalkable(new IntPoint3D(l, z)),
				l => 0);
			var dirs = res.GetPath();

			m_pathDirs = new Queue<Direction>(dirs);

			if (m_pathDirs.Count == 0)
			{
				m_pathDirs = null;
				return Progress.Fail;
			}

			m_supposedLocation = this.Worker.Location;

			return Progress.Ok;
		}

		Progress CheckProgress()
		{
			var v = m_dest - this.Worker.Location;
			if ((m_adjacent && v.IsAdjacent2D) || (!m_adjacent && v.IsNull))
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return "Move";
		}

	}

	class MineActionJob : ActionJob
	{
		IntPoint3D m_location;
		Environment m_environment;

		public MineActionJob(IJob job, Environment environment, IntPoint3D location)
			: base(job)
		{
			m_environment = environment;
			m_location = location;
		}

		protected override void Cleanup()
		{
			m_environment = null;
		}

		protected override Progress PrepareNextActionOverride()
		{
			var v = m_location - this.Worker.Location;

			if (!v.IsAdjacent2D)
				return Progress.Fail;

			if (CheckProgress() == Progress.Done)
				return Progress.Done;

			var action = new MineAction(v.ToDirection());

			this.CurrentAction = action;

			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressEvent e)
		{
			if (e.Success == false)
				return Progress.Fail;

			if (e.TicksLeft > 0)
				return Progress.Ok;

			return CheckProgress();
		}

		Progress CheckProgress()
		{
			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;
			else
				return Progress.Ok;
		}

		public override string ToString()
		{
			return "Mine";
		}
	}

	class BuildItemActionJob : ActionJob
	{
		ItemObject[] m_items;

		public BuildItemActionJob(IJob parent, ItemObject[] items)
			: base(parent)
		{
			m_items = items;
		}

		protected override void Cleanup()
		{
			m_items = null;
		}

		protected override Progress PrepareNextActionOverride()
		{
			var action = new BuildItemAction(m_items);
			this.CurrentAction = action;
			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressEvent e)
		{
			if (e.Success == false)
				return Progress.Fail;

			if (e.TicksLeft > 0)
				return Progress.Ok;

			return Progress.Done;
		}

		public override string ToString()
		{
			return "BuildItemActionJob";
		}
	}

	class GetItemActionJob : ActionJob
	{
		ItemObject m_item;

		public GetItemActionJob(IJob parent, ItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		protected override Progress PrepareNextActionOverride()
		{
			var action = new GetAction(new ItemObject[] { m_item });
			this.CurrentAction = action;
			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressEvent e)
		{
			if (e.Success == false)
				return Progress.Fail;

			if (e.TicksLeft > 0)
				return Progress.Ok;

			return Progress.Done;
		}

		public override string ToString()
		{
			return "GetItemActionJob";
		}
	}

	class DropItemActionJob : ActionJob
	{
		ItemObject m_item;

		public DropItemActionJob(IJob parent, ItemObject item)
			: base(parent)
		{
			m_item = item;
		}

		protected override Progress PrepareNextActionOverride()
		{
			var action = new DropAction(new ItemObject[] { m_item });
			this.CurrentAction = action;
			return Progress.Ok;
		}

		protected override Progress ActionProgressOverride(ActionProgressEvent e)
		{
			if (e.Success == false)
				return Progress.Fail;

			if (e.TicksLeft > 0)
				return Progress.Ok;

			return Progress.Done;
		}

		public override string ToString()
		{
			return "DropItemActionJob";
		}
	}
}
