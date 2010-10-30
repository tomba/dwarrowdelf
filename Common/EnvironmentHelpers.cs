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
				var l = p + dir;
				if (CanMoveTo(env, p, l))
					yield return dir;
			}

			if (CanMoveTo(env, p, p + Direction.Up))
				yield return Direction.Up;

			if (CanMoveTo(env, p, p + Direction.Down))
				yield return Direction.Down;

			foreach (var dir in DirectionExtensions.CardinalDirections)
			{
				var d = dir | Direction.Down;
				if (CanMoveTo(env, p, p + d))
					yield return d;

				d = dir | Direction.Up;
				if (CanMoveTo(env, p, p + d))
					yield return d;
			}
		}

		public static bool CanMoveTo(IEnvironment env, IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			if (!env.Bounds.Contains(dstLoc))
				return false;

			IntVector3D v = dstLoc - srcLoc;

			if (!v.IsNormal)
				throw new Exception();

			var dstInter = env.GetInterior(dstLoc);
			var dstFloor = env.GetFloor(dstLoc);

			if (dstInter.Blocker || !dstFloor.IsCarrying)
				return false;

			if (v.Z == 0)
				return true;

			Direction dir = v.ToDirection();

			var srcInter = env.GetInterior(srcLoc);
			var srcFloor = env.GetFloor(srcLoc);

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs && dstFloor.ID == FloorID.Hole;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs && srcFloor.ID == FloorID.Hole;

			var d2d = v.ToIntVector().ToDirection();

			if (dir.ContainsUp())
			{
				var tileAboveSlope = env.GetTileData(srcLoc + Direction.Up);
				return d2d.IsCardinal() && srcFloor.ID.IsSlope() && srcFloor.ID == d2d.ToSlope() && tileAboveSlope.IsEmpty;
			}

			if (dir.ContainsDown())
			{
				var tileAboveSlope = env.GetTileData(dstLoc + Direction.Up);
				return d2d.IsCardinal() && dstFloor.ID.IsSlope() && dstFloor.ID == d2d.Reverse().ToSlope() && tileAboveSlope.IsEmpty;
			}

			return false;
		}
	}
}
