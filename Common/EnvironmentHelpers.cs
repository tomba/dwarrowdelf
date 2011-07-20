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
			var terrain = env.GetTerrain(p);

			if (inter.IsBlocker || !terrain.IsSupporting)
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
			var srcTerrain = env.GetTerrain(srcLoc);

			if (srcInter.IsBlocker || srcTerrain.IsBlocker)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs;

			if (dir == Direction.Down)
				return srcTerrain.ID == TerrainID.Hole;

			if (dir.ContainsDown())
			{
				return true;
			}

			if (dir.ContainsUp())
			{
				if (!srcTerrain.ID.IsSlope())
					return false;

				if (env.GetTerrainID(srcLoc + Direction.Up) != TerrainID.Empty)
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
			if (!env.Contains(dstLoc))
				return false;

			var dstTerrain = env.GetTerrain(dstLoc);
			var dstInter = env.GetInterior(dstLoc);

			if (dstInter.IsBlocker || dstTerrain.IsBlocker || !dstTerrain.IsSupporting)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return dstTerrain.ID == TerrainID.Hole;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs;

			if (dir.ContainsUp())
			{
				return true;
			}

			if (dir.ContainsDown())
			{
				if (!dstTerrain.ID.IsSlope())
					return false;

				if (env.GetTerrainID(dstLoc + Direction.Up) != TerrainID.Empty)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Tile can be entered and stood upon
		/// </summary>
		public static bool CanEnter(IEnvironment env, IntPoint3D location)
		{
			if (!env.Contains(location))
				return false;

			var dstTerrain = env.GetTerrain(location);
			var dstInter = env.GetInterior(location);

			return dstTerrain.IsSupporting && !dstTerrain.IsBlocker && !dstInter.IsBlocker;
		}
	}
}
