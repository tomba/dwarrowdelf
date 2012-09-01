using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class EnvironmentHelpers
	{
		/// <summary>
		/// Return all possible move directions.
		/// XXX Some room for optimization...
		/// </summary>
		public static IEnumerable<Direction> GetDirectionsFrom(IEnvironmentObject env, IntPoint3 p)
		{
			foreach (var dir in DirectionExtensions.PlanarUpDownDirections)
			{
				var d = AdjustMoveDir(env, p, dir);
				if (d != Direction.None)
					yield return d;
			}
		}

		/// <summary>
		/// Determine if a living can move from its current location to dir
		/// </summary>
		public static bool CanMoveFromTo(ILivingObject living, Direction dir)
		{
			var env = living.Environment;
			var src = living.Location;
			var dst = living.Location + dir;
			return CanMoveFrom(env, src, dir) && CanMoveTo(env, dst, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir
		/// </summary>
		public static bool CanMoveFromTo(IEnvironmentObject env, IntPoint3 srcLoc, Direction dir)
		{
			var dstLoc = srcLoc + dir;
			return CanMoveFrom(env, srcLoc, dir) && CanMoveTo(env, dstLoc, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir, without considering the destination
		/// </summary>
		public static bool CanMoveFrom(IEnvironmentObject env, IntPoint3 srcLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

			if (env.Contains(srcLoc) == false)
				return false;

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
		public static bool CanMoveTo(IEnvironmentObject env, IntPoint3 dstLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

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
		public static bool CanEnter(IEnvironmentObject env, IntPoint3 location)
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
		public static bool CanSeeThrough(IEnvironmentObject env, IntPoint3 location)
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
		public static bool CanSeeThroughDown(IEnvironmentObject env, IntPoint3 location)
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
		public static bool CanBeSeen(IEnvironmentObject env, IntPoint3 location)
		{
			bool hidden = true;

			foreach (var d in DirectionExtensions.PlanarDirections)
			{
				var p = location + d;

				if (EnvironmentHelpers.CanSeeThrough(env, p))
				{
					hidden = false;
					break;
				}
			}

			if (hidden)
				hidden = !EnvironmentHelpers.CanSeeThroughDown(env, location + Direction.Up);

			return !hidden;
		}

		/// <summary>
		/// For PlanarUpDown directions, return Direction.None if the direction cannot be entered,
		/// or the direction, adjusted by slopes (i.e. or'ed with Up or Down)
		/// </summary>
		public static Direction AdjustMoveDir(IEnvironmentObject env, IntPoint3 location, Direction dir)
		{
			Debug.Assert(dir.IsValid());
			Debug.Assert(dir != Direction.None);
			Debug.Assert(dir.IsPlanarUpDown());

			if (EnvironmentHelpers.CanMoveFromTo(env, location, dir))
				return dir;

			if (dir == Direction.Up || dir == Direction.Down)
				return Direction.None;

			if (EnvironmentHelpers.CanMoveFromTo(env, location, dir | Direction.Up))
				return dir | Direction.Up;

			if (EnvironmentHelpers.CanMoveFromTo(env, location, dir | Direction.Down))
				return dir | Direction.Down;

			return Direction.None;
		}
	}
}
