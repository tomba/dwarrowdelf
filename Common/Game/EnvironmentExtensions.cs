using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public static class EnvironmentExtensions
	{
		/// <summary>
		/// Return all possible move directions.
		/// XXX Some room for optimization...
		/// </summary>
		public static IEnumerable<Direction> GetDirectionsFrom(this IEnvironmentObject env, IntVector3 p)
		{
			foreach (var dir in DirectionExtensions.PlanarUpDownDirections)
			{
				var d = AdjustMoveDir(env, p, dir);
				if (d != Direction.None)
					yield return d;
			}
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir
		/// </summary>
		public static bool CanMoveFromTo(this IEnvironmentObject env, IntVector3 srcLoc, Direction dir)
		{
			var dstLoc = srcLoc + dir;
			return CanMoveFrom(env, srcLoc, dir) && CanMoveTo(env, dstLoc, dir);
		}

		/// <summary>
		/// Determine if a living can move from srcLoc to dir, without considering the destination
		/// </summary>
		public static bool CanMoveFrom(this IEnvironmentObject env, IntVector3 srcLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

			if (env.Contains(srcLoc) == false)
				return false;

			var td = env.GetTileData(srcLoc);

			if (td.IsUndefined)
				return false;

			// Perhaps this check is not needed
			if (td.IsWalkable == false)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return td.ID == TileID.Stairs;

			if (dir == Direction.Down)
				return true;

			if (dir.ContainsDown())
			{
				return true;
			}

			if (dir.ContainsUp() && env.Size.Depth > srcLoc.Z + 1)
			{
				if (env.GetTileData(srcLoc.Up).IsEmpty == false)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Determine if a living can move to dir, ending to dstLoc, without considering the source
		/// </summary>
		public static bool CanMoveTo(this IEnvironmentObject env, IntVector3 dstLoc, Direction dir)
		{
			Debug.Assert(dir.IsValid());

			if (!env.Contains(dstLoc))
				return false;

			var td = env.GetTileData(dstLoc);

			if (td.IsUndefined)
				return false;

			if (td.IsWalkable == false)
				return false;

			if (dir.IsPlanar())
				return true;

			if (dir == Direction.Up)
				return true;

			if (dir == Direction.Down)
				return td.ID == TileID.Stairs;

			if (dir.ContainsUp())
			{
				return true;
			}

			if (dir.ContainsDown() && env.Size.Depth > dstLoc.Z + 1)
			{
				if (env.GetTileData(dstLoc.Up).IsEmpty == false)
					return false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Tile can be entered and stood upon
		/// </summary>
		public static bool CanEnter(this IEnvironmentObject env, IntVector3 location)
		{
			if (!env.Contains(location))
				return false;

			var td = env.GetTileData(location);

			if (td.IsUndefined)
				return false;

			return td.IsWalkable;
		}

		/// <summary>
		/// Can the given tile be seen from any adjacent tile
		/// </summary>
		public static bool CanBeSeen(this IEnvironmentObject env, IntVector3 location)
		{
			foreach (var d in DirectionExtensions.PlanarUpDownDirections)
			{
				var p = location + d;
				if (env.Contains(p) && env.GetTileData(p).IsSeeThrough)
					return true;
			}

			return false;
		}

		/// <summary>
		/// For PlanarUpDown directions, return Direction.None if the direction cannot be entered,
		/// or the direction, adjusted by slopes (i.e. or'ed with Up or Down)
		/// </summary>
		public static Direction AdjustMoveDir(this IEnvironmentObject env, IntVector3 location, Direction dir)
		{
			Debug.Assert(dir.IsValid());
			Debug.Assert(dir != Direction.None);
			Debug.Assert(dir.IsPlanarUpDown());

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir))
				return dir;

			if (dir == Direction.Up || dir == Direction.Down)
				return Direction.None;

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir | Direction.Up))
				return dir | Direction.Up;

			if (EnvironmentExtensions.CanMoveFromTo(env, location, dir | Direction.Down))
				return dir | Direction.Down;

			return Direction.None;
		}

		/// <summary>
		/// Return enterable positions around the given location, based on positioning
		/// </summary>
		public static IEnumerable<IntVector3> GetPositioningLocations(this IEnvironmentObject env, IntVector3 pos,
			DirectionSet positioning)
		{
			return positioning.ToSurroundingPoints(pos).Where(p => CanEnter(env, p));
		}

		/// <summary>
		/// Get possible positioning to perform mining to given location
		/// </summary>
		public static DirectionSet GetPossibleMiningPositioning(this IEnvironmentObject env, IntVector3 p,
			MineActionType mineActionType)
		{
			DirectionSet ds;

			switch (mineActionType)
			{
				case MineActionType.Mine:
					ds = DirectionSet.Planar;

					if (env.GetTileData(p.Up).IsClear)
					{
						ds |= DirectionSet.DownNorth |
							DirectionSet.DownEast |
							DirectionSet.DownSouth |
							DirectionSet.DownWest;
					}

					break;

				case MineActionType.Stairs:
					ds = DirectionSet.Planar | DirectionSet.Down;

					if (env.CanMoveFrom(p.Down, Direction.Up))
						ds |= DirectionSet.Up;

					break;

				default:
					throw new Exception();
			}

			return ds;
		}
	}
}
