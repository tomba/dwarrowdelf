using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dwarrowdelf
{
	public static class DirectionConsts
	{
		public const int Mask = 0x3;

		public const int XShift = 0;
		public const int YShift = 2;
		public const int ZShift = 4;

		public const int DirPos = 1 << 0;
		public const int DirNeg = 1 << 1;
	}

	[Flags]
	public enum Direction : byte
	{
		None = 0,

		North = DirectionConsts.DirPos << DirectionConsts.YShift,
		South = DirectionConsts.DirNeg << DirectionConsts.YShift,
		West = DirectionConsts.DirNeg << DirectionConsts.XShift,
		East = DirectionConsts.DirPos << DirectionConsts.XShift,
		Up = DirectionConsts.DirPos << DirectionConsts.ZShift,
		Down = DirectionConsts.DirNeg << DirectionConsts.ZShift,

		NorthWest = North | West,
		NorthEast = North | East,
		SouthWest = South | West,
		SouthEast = South | East,
	}

	[Flags]
	public enum DirectionSet
	{
		None = 0,

		/* Z = 0 */
		/* Y = 0 */
		Exact = 1 << 0,
		East = 1 << 1,
		West = 1 << 2,

		/* Y = 1 */
		North = 1 << 3,
		NorthEast = 1 << 4,
		NorthWest = 1 << 5,

		/* Y = 2 */
		South = 1 << 6,
		SouthEast = 1 << 7,
		SouthWest = 1 << 8,

		/* Z = 1 */
		/* Y = 0 */
		Up = 1 << 9,
		UpEast = 1 << 10,
		UpWest = 1 << 11,

		/* Y = 1 */
		UpNorth = 1 << 12,
		UpNorthEast = 1 << 13,
		UpNorthWest = 1 << 14,

		/* Y = 2 */
		UpSouth = 1 << 15,
		UpSouthEast = 1 << 16,
		UpSouthWest = 1 << 17,

		/* Z = 2 */
		/* Y = 0 */
		Down = 1 << 18,
		DownEast = 1 << 19,
		DownWest = 1 << 20,

		/* Y = 1 */
		DownNorth = 1 << 21,
		DownNorthEast = 1 << 22,
		DownNorthWest = 1 << 23,

		/* Y = 2 */
		DownSouth = 1 << 24,
		DownSouthEast = 1 << 25,
		DownSouthWest = 1 << 26,

		Cardinal = North | East | South | West,
		Planar = North | NorthEast | East | SouthEast | South | SouthWest | West | NorthWest,
		CardinalUpDown = Cardinal | Up | Down,
		PlanarUpDown = Planar | Up | Down,

		Any = ((1 << 27) - 1),
	}

	public static class DirectionExtensions
	{
		public static bool Contains(this DirectionSet dirset, Direction dir)
		{
			int xSeg = ((int)dir >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int ySeg = ((int)dir >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int zSeg = ((int)dir >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			int bit = zSeg * 9 + ySeg * 3 + xSeg;

			return (((int)dirset) & (1 << bit)) != 0;
		}

		public static IEnumerable<Direction> ToDirections(this DirectionSet dirset)
		{
			List<Direction> dirs = new List<Direction>();

			int ds = (int)dirset;

			for (int i = 0; i < 27; ++i)
			{
				if ((ds & (1 << i)) == 0)
					continue;

				int z = i / 9;
				int y = (i % 9) / 3;
				int x = (i % 3);

				int d = 0;
				d |= x << DirectionConsts.XShift;
				d |= y << DirectionConsts.YShift;
				d |= z << DirectionConsts.ZShift;

				dirs.Add((Direction)d);
			}

			return dirs;
		}

		/// <summary>
		/// Cardinal Directions (4)
		/// </summary>
		public static readonly ReadOnlyCollection<Direction> CardinalDirections;
		/// <summary>
		/// Intercardinal Directions (4)
		/// </summary>
		public static readonly ReadOnlyCollection<Direction> IntercardinalDirections;
		/// <summary>
		/// Planar Directions (8)
		/// </summary>
		public static readonly ReadOnlyCollection<Direction> PlanarDirections;
		/// <summary>
		/// Cardinal + Up & Down (6)
		/// </summary>
		public static readonly ReadOnlyCollection<Direction> CardinalUpDownDirections;
		/// <summary>
		/// Planar Directions + Up & Down (10)
		/// </summary>
		public static readonly ReadOnlyCollection<Direction> PlanarUpDownDirections;

		static DirectionExtensions()
		{
			CardinalDirections = Array.AsReadOnly(new Direction[] {
				Direction.North,
				Direction.East,
				Direction.South,
				Direction.West,
			});

			IntercardinalDirections = Array.AsReadOnly(new Direction[] {
				Direction.NorthEast,
				Direction.SouthEast,
				Direction.SouthWest,
				Direction.NorthWest,
			});

			PlanarDirections = Array.AsReadOnly(new Direction[] {
				Direction.North,
				Direction.NorthEast,
				Direction.East,
				Direction.SouthEast,
				Direction.South,
				Direction.SouthWest,
				Direction.West,
				Direction.NorthWest,
			});

			CardinalUpDownDirections = Array.AsReadOnly(new Direction[] {
				Direction.North,
				Direction.East,
				Direction.South,
				Direction.West,
				Direction.Up,
				Direction.Down,
			});

			PlanarUpDownDirections = Array.AsReadOnly(new Direction[] {
				Direction.North,
				Direction.NorthEast,
				Direction.East,
				Direction.SouthEast,
				Direction.South,
				Direction.SouthWest,
				Direction.West,
				Direction.NorthWest,
				Direction.Up,
				Direction.Down,
	});
		}

		public static Direction Reverse(this Direction dir)
		{
			uint d = (uint)dir;

			if ((d & (DirectionConsts.Mask << DirectionConsts.XShift)) != 0)
				d ^= DirectionConsts.Mask << DirectionConsts.XShift;

			if ((d & (DirectionConsts.Mask << DirectionConsts.YShift)) != 0)
				d ^= DirectionConsts.Mask << DirectionConsts.YShift;

			if ((d & (DirectionConsts.Mask << DirectionConsts.ZShift)) != 0)
				d ^= DirectionConsts.Mask << DirectionConsts.ZShift;

			return (Direction)d;
		}

		public static bool IsCardinal(this Direction dir)
		{
			return dir == Direction.North || dir == Direction.East || dir == Direction.South || dir == Direction.West;
		}

		public static bool IsIntercardinal(this Direction dir)
		{
			return dir == Direction.NorthEast || dir == Direction.SouthEast || dir == Direction.SouthWest || dir == Direction.NorthWest;
		}

		public static bool IsPlanar(this Direction dir)
		{
			return IsCardinal(dir) || IsIntercardinal(dir);
		}

		public static bool IsCardinalUpDown(this Direction dir)
		{
			return IsCardinal(dir) || dir == Direction.Up || dir == Direction.Down;
		}

		public static bool IsPlanarUpDown(this Direction dir)
		{
			return IsPlanar(dir) || dir == Direction.Up || dir == Direction.Down;
		}

		public static bool ContainsUp(this Direction dir)
		{
			return (dir & Direction.Up) != 0;
		}

		public static bool ContainsDown(this Direction dir)
		{
			return (dir & Direction.Down) != 0;
		}

		public static Direction ToPlanarDirection(this Direction dir)
		{
			int d = (int)dir;
			d &= ~(DirectionConsts.Mask << DirectionConsts.ZShift);
			return (Direction)d;
		}
	}
}
