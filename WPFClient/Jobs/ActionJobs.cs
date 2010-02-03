using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MyGame.Client
{
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

		protected override void AbortOverride()
		{
			m_pathDirs = null;
			m_numFails = 0;
		}

		protected override Progress AssignOverride(Living worker)
		{
			m_numFails = 0;
			return Progress.Ok;
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
			m_supposedLocation += new IntVector3D(dir);

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

			var res = AStar.AStar3D.Find(this.Worker.Location, m_dest, !m_adjacent, l => 0, GetTileDirs);
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

		IEnumerable<Direction> GetTileDirs(IntPoint3D p)
		{
			var env = m_environment;

			foreach (var v in IntVector.GetAllXYDirections())
			{
				var l = p + v;
				if (env.IsWalkable(l) == true && env.Bounds.Contains(l))
					yield return v.ToDirection();
			}

			if (env.GetInterior(p).ID == InteriorID.Stairs)
				yield return Direction.Up;

			if (env.GetInterior(p + Direction.Down).ID == InteriorID.Stairs)
				yield return Direction.Down;

			if (env.GetInterior(p).ID.IsSlope())
			{
				var dir = InteriorExtensions.GetDirFromSlope(env.GetInterior(p).ID);
				dir |= Direction.Up;
				yield return dir;
			}

			foreach (var dir in DirectionExtensions.GetCardinalDirections())
			{
				var d = dir | Direction.Down;
				var id = env.GetInterior(p + d).ID;
				if (id.IsSlope() && InteriorExtensions.GetDirFromSlope(id) == dir.Reverse())
					yield return d;
			}
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
