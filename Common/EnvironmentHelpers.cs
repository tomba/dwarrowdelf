using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class EnvironmentHelpers
	{
		/* XXX some room for optimization... */
		public static IEnumerable<Direction> GetDirectionsFrom(IEnvironmentObject env, IntPoint3D p)
		{
			var td = env.GetTileData(p);

			var terrain = Terrains.GetTerrain(td.TerrainID);
			var inter = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			if (inter.IsBlocker || !terrain.IsSupporting || itemBlocks)
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
		public static bool CanMoveFromTo(IEnvironmentObject env, IntPoint3D srcLoc, Direction dir)
		{
			var dstLoc = srcLoc + dir;
			return CanMoveFrom(env, srcLoc, dir) && CanMoveTo(env, dstLoc, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir, without considering the destination
		/// </summary>
		public static bool CanMoveFrom(IEnvironmentObject env, IntPoint3D srcLoc, Direction dir)
		{
			var td = env.GetTileData(srcLoc);

			if (td.TerrainID == TerrainID.Undefined || td.InteriorID == InteriorID.Undefined)
				return false;

			var srcTerrain = Terrains.GetTerrain(td.TerrainID);
			var srcInter = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			if (srcInter.IsBlocker || srcTerrain.IsBlocker || itemBlocks)
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
		public static bool CanMoveTo(IEnvironmentObject env, IntPoint3D dstLoc, Direction dir)
		{
			if (!env.Contains(dstLoc))
				return false;

			var td = env.GetTileData(dstLoc);

			if (td.TerrainID == TerrainID.Undefined || td.InteriorID == InteriorID.Undefined)
				return false;

			var dstTerrain = Terrains.GetTerrain(td.TerrainID);
			var dstInter = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			if (dstInter.IsBlocker || dstTerrain.IsBlocker || !dstTerrain.IsSupporting || itemBlocks)
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
		public static bool CanEnter(IEnvironmentObject env, IntPoint3D location)
		{
			if (!env.Contains(location))
				return false;

			var td = env.GetTileData(location);

			if (td.TerrainID == TerrainID.Undefined || td.InteriorID == InteriorID.Undefined)
				return false;

			var terrain = Terrains.GetTerrain(td.TerrainID);
			var interior = Interiors.GetInterior(td.InteriorID);
			var itemBlocks = (td.Flags & TileFlags.ItemBlocks) != 0;

			return terrain.IsSupporting && !terrain.IsBlocker && !interior.IsBlocker && !itemBlocks;
		}

		/// <summary>
		/// Can the tiles around the given tile be seen
		/// </summary>
		public static bool CanSeeThrough(IEnvironmentObject env, IntPoint3D location)
		{
			if (!env.Contains(location))
				return false;

			var td = env.GetTileData(location);

			if (td.TerrainID == TerrainID.Undefined || td.InteriorID == InteriorID.Undefined)
				return false;

			var terrain = Terrains.GetTerrain(td.TerrainID);
			var interior = Interiors.GetInterior(td.InteriorID);

			return terrain.IsSeeThrough && interior.IsSeeThrough;
		}

		/// <summary>
		/// Can the tile below the given tile be seen
		/// </summary>
		public static bool CanSeeThroughDown(IEnvironmentObject env, IntPoint3D location)
		{
			if (!env.Contains(location))
				return false;

			var td = env.GetTileData(location);

			if (td.TerrainID == TerrainID.Undefined || td.InteriorID == InteriorID.Undefined)
				return false;

			var terrain = Terrains.GetTerrain(td.TerrainID);
			var interior = Interiors.GetInterior(td.InteriorID);

			return terrain.IsSeeThroughDown && interior.IsSeeThrough;
		}

		/// <summary>
		/// Can the given tile be seen from any direction
		/// </summary>
		public static bool CanBeSeen(IEnvironmentObject env, IntPoint3D location)
		{
			bool hidden = DirectionExtensions.PlanarDirections
				.Select(d => location + d)
				.All(p => !EnvironmentHelpers.CanSeeThrough(env, p));

			if (hidden)
				hidden = !EnvironmentHelpers.CanSeeThroughDown(env, location + Direction.Up);

			return !hidden;
		}
	}
}
