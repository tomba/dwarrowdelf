using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class EnvironmentHelpers
	{
		/* XXX some room for optimization... */
		public static IEnumerable<Direction> GetDirectionsFrom(IEnvironment env, IntPoint3D p)
		{
			var inter = env.GetInterior(p);
			var floor = env.GetFloor(p);

			if (inter.Blocker || !floor.IsCarrying)
				yield break;

			foreach (var dir in DirectionExtensions.PlanarDirections)
			{
				if (CanMoveFromTo(env, p, dir))
					yield return dir;
			}

			if (CanMoveFromTo(env, p, Direction.Up))
				yield return Direction.Up;

			if (CanMoveFromTo(env, p, Direction.Down))
				yield return Direction.Down;

			foreach (var dir in DirectionExtensions.PlanarDirections)
			{
				var d = dir | Direction.Down;
				if (CanMoveFromTo(env, p, d))
					yield return d;

				d = dir | Direction.Up;
				if (CanMoveFromTo(env, p, d))
					yield return d;
			}
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir
		/// </summary>
		public static bool CanMoveFromTo(IEnvironment env, IntPoint3D srcLoc, Direction dir)
		{
			var dstLoc = srcLoc + dir;
			return CanMoveFrom(env, srcLoc, dir) && CanMoveTo(env, dstLoc, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir, without considering the destination
		/// </summary>
		public static bool CanMoveFrom(IEnvironment env, IntPoint3D srcLoc, Direction dir)
		{
			var srcInter = env.GetInterior(srcLoc);

			if (srcInter.Blocker)
				return false;

			if (dir.IsPlanar())
				return true;

			var srcFloor = env.GetFloor(srcLoc);

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs;

			if (dir == Direction.Down)
				return srcFloor.ID == FloorID.Hole;

			if (dir.ContainsDown())
			{
				return true;
			}

			if (dir.ContainsUp())
			{
				if (!srcFloor.ID.IsSlope())
					return false;

				var tileAboveSlope = env.GetTileData(srcLoc + Direction.Up);
				if (!tileAboveSlope.IsEmpty)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Determine if a living can move to dir to dstLoc, without considering the source
		/// </summary>
		public static bool CanMoveTo(IEnvironment env, IntPoint3D dstLoc, Direction dir)
		{
			if (!env.Bounds.Contains(dstLoc))
				return false;

			var dstInter = env.GetInterior(dstLoc);
			var dstFloor = env.GetFloor(dstLoc);

			if (dstInter.Blocker || !dstFloor.IsCarrying)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return dstFloor.ID == FloorID.Hole;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs;

			if (dir.ContainsUp())
			{
				return true;
			}

			if (dir.ContainsDown())
			{
				if (!dstFloor.ID.IsSlope())
					return false;

				var tileAboveSlope = env.GetTileData(dstLoc + Direction.Up);
				if (!tileAboveSlope.IsEmpty)
					return false;

				return true;
			}

			return false;
		}
	}
}
