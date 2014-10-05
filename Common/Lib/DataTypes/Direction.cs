using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf
{
	static class DirectionConsts
	{
		public const int Mask = 0x3;
		public const int FullMask = (Mask << XShift) | (Mask << YShift) | (Mask << ZShift);

		// Note: using values 0, 1, 3 would give us much simpler conversions,
		// but then we cannot set DirNeg and DirPos at the same time
		// ((x ^ 1) - (x >> 1)) - 1
		public const int DirNone = 0;			// ((0 ^ 1) - (0 >> 1)) - 1 = 0
		public const int DirNeg = (1 << 0);		// ((1 ^ 1) - (1 >> 1)) - 1 = -1
		public const int DirPos = (1 << 1);		// ((2 ^ 1) - (2 >> 1)) - 1 = 1

		public const int XShift = 0;
		public const int YShift = 2;
		public const int ZShift = 4;
	}

	public enum DirectionOrdinal
	{
		NegativeX = 0,
		PositiveX = 1,
		NegativeY = 2,
		PositiveY = 3,
		NegativeZ = 4,
		PositiveZ = 5,

		West = NegativeX,
		East = PositiveX,
		North = NegativeY,
		South = PositiveY,
		Down = NegativeZ,
		Up = PositiveZ,
	}

	[Flags]
	public enum Direction : byte
	{
		None = 0,

		NegativeX = DirectionConsts.DirNeg << DirectionConsts.XShift,	// 1 << 0
		PositiveX = DirectionConsts.DirPos << DirectionConsts.XShift,	// 1 << 1
		NegativeY = DirectionConsts.DirNeg << DirectionConsts.YShift,	// 1 << 2
		PositiveY = DirectionConsts.DirPos << DirectionConsts.YShift,	// 1 << 3
		NegativeZ = DirectionConsts.DirNeg << DirectionConsts.ZShift,	// 1 << 4
		PositiveZ = DirectionConsts.DirPos << DirectionConsts.ZShift,	// 1 << 5

		West = NegativeX,
		East = PositiveX,
		North = NegativeY,
		South = PositiveY,
		Down = NegativeZ,
		Up = PositiveZ,

		NorthWest = North | West,
		NorthEast = North | East,
		SouthWest = South | West,
		SouthEast = South | East,
	}

	[Flags]
	public enum DirectionSet
	{
		None = 0,

		DownNorthWest = 1 << 0,		// Z=0 Y=0 X=0
		DownNorth = 1 << 1,			// Z=0 Y=0 X=1
		DownNorthEast = 1 << 2,		// Z=0 Y=0 X=2
		DownWest = 1 << 3,			// Z=0 Y=1 X=0
		Down = 1 << 4,				// Z=0 Y=1 X=1
		DownEast = 1 << 5,			// Z=0 Y=1 X=2
		DownSouthWest = 1 << 6,		// Z=0 Y=2 X=0
		DownSouth = 1 << 7,			// Z=0 Y=2 X=1
		DownSouthEast = 1 << 8,		// Z=0 Y=2 X=2

		NorthWest = 1 << 9,			// Z=1 Y=0 X=0
		North = 1 << 10,			// Z=1 Y=0 X=1
		NorthEast = 1 << 11,		// Z=1 Y=0 X=2
		West = 1 << 12,				// Z=1 Y=1 X=0
		Exact = 1 << 13,			// Z=1 Y=1 X=1
		East = 1 << 14,				// Z=1 Y=1 X=2
		SouthWest = 1 << 15,		// Z=1 Y=2 X=0
		South = 1 << 16,			// Z=1 Y=2 X=1
		SouthEast = 1 << 17,		// Z=1 Y=2 X=2

		UpNorthWest = 1 << 18,		// Z=2 Y=0 X=0
		UpNorth = 1 << 19,			// Z=2 Y=0 X=1
		UpNorthEast = 1 << 20,		// Z=2 Y=0 X=2
		UpWest = 1 << 21,			// Z=2 Y=1 X=0
		Up = 1 << 22,				// Z=2 Y=1 X=1
		UpEast = 1 << 23,			// Z=2 Y=1 X=2
		UpSouthWest = 1 << 24,		// Z=2 Y=2 X=0
		UpSouth = 1 << 25,			// Z=2 Y=2 X=1
		UpSouthEast = 1 << 26,		// Z=2 Y=2 X=2

		Cardinal = North | East | South | West,
		Planar = North | NorthEast | East | SouthEast | South | SouthWest | West | NorthWest,
		CardinalUpDown = Cardinal | Up | Down,
		PlanarUpDown = Planar | Up | Down,
		Intercardinal = NorthEast | SouthEast | SouthWest | NorthWest,

		All = ((1 << 27) - 1),
	}

	public static class DirectionExtensions
	{
		public static DirectionOrdinal ToDirectionOrdinal(this Direction dir)
		{
			switch (dir)
			{
				case Direction.West:
					return DirectionOrdinal.West;
				case Direction.East:
					return DirectionOrdinal.East;
				case Direction.North:
					return DirectionOrdinal.North;
				case Direction.South:
					return DirectionOrdinal.South;
				case Direction.Down:
					return DirectionOrdinal.Down;
				case Direction.Up:
					return DirectionOrdinal.Up;
				default:
					throw new Exception();
			}
		}

		public static bool Contains(this DirectionSet dirset, Direction dir)
		{
			var ds = dir.ToDirectionSet();
			return (dirset & ds) != 0;
		}

		public static void DirectionToComponents(this Direction dir, out int x, out int y, out int z)
		{
			int d = (int)dir;

			x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			x = (x ^ 1) - (x >> 1) - 1;
			y = (y ^ 1) - (y >> 1) - 1;
			z = (z ^ 1) - (z >> 1) - 1;
		}

		public static Direction ComponentsToDirection(int x, int y, int z)
		{
			x += 1;
			y += 1;
			z += 1;

			int d = 0;

			d |= ((x ^ 1) - (x >> 1)) << DirectionConsts.XShift;
			d |= ((y ^ 1) - (y >> 1)) << DirectionConsts.YShift;
			d |= ((z ^ 1) - (z >> 1)) << DirectionConsts.ZShift;

			return (Direction)d;
		}

		public static void DirectionToComponents(this Direction dir, out int x, out int y)
		{
			int d = (int)dir;

			x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;

			x = (x ^ 1) - (x >> 1) - 1;
			y = (y ^ 1) - (y >> 1) - 1;
		}

		public static Direction ComponentsToDirection(int x, int y)
		{
			x += 1;
			y += 1;

			int d = 0;

			d |= ((x ^ 1) - (x >> 1)) << DirectionConsts.XShift;
			d |= ((y ^ 1) - (y >> 1)) << DirectionConsts.YShift;

			return (Direction)d;
		}

		public static DirectionSet ToDirectionSet(this Direction dir)
		{
			int x, y, z;

			DirectionToComponents(dir, out x, out y, out z);

			x += 1;
			y += 1;
			z += 1;

			int bit = z * 9 + y * 3 + x;

			return (DirectionSet)(1 << bit);
		}

		public static IEnumerable<Direction> ToDirections(this DirectionSet dirset)
		{
			int ds = (int)dirset;

			for (int i = 0; i < 27; ++i)
			{
				if ((ds & (1 << i)) == 0)
					continue;

				int z = i / 9 - 1;
				int y = (i % 9) / 3 - 1;
				int x = (i % 3) - 1;

				yield return ComponentsToDirection(x, y, z);
			}
		}

		public static IEnumerable<IntVector3> ToVectors(this DirectionSet dirset)
		{
			int ds = (int)dirset;

			for (int i = 0; i < 27; ++i)
			{
				if ((ds & (1 << i)) == 0)
					continue;

				int z = i / 9;
				int y = (i % 9) / 3;
				int x = (i % 3);

				x = x - 1;
				y = y - 1;
				z = z - 1;

				yield return new IntVector3(x, y, z);
			}
		}

		public static IEnumerable<IntVector3> ToSurroundingPoints(this DirectionSet dirset, IntVector3 p)
		{
			foreach (var v in dirset.ToVectors())
				yield return p + v;
		}

		public static IEnumerable<IntVector2> ToSurroundingPoints(this DirectionSet dirset, IntVector2 p)
		{
			int ds = (int)dirset;

			for (int i = 0; i < 27; ++i)
			{
				if ((ds & (1 << i)) == 0)
					continue;

				int y = (i % 9) / 3;
				int x = (i % 3);

				x = x - 1;
				y = y - 1;

				yield return new IntVector2(p.X + x, p.Y + y);
			}
		}

		/// <summary>
		/// Cardinal Directions (4)
		/// </summary>
		public static readonly Direction[] CardinalDirections;
		/// <summary>
		/// Intercardinal Directions (4)
		/// </summary>
		public static readonly Direction[] IntercardinalDirections;
		/// <summary>
		/// Planar Directions (8)
		/// </summary>
		public static readonly Direction[] PlanarDirections;
		/// <summary>
		/// Cardinal + Up + Down (6)
		/// </summary>
		public static readonly Direction[] CardinalUpDownDirections;
		/// <summary>
		/// Planar Directions + Up + Down (10)
		/// </summary>
		public static readonly Direction[] PlanarUpDownDirections;

		static DirectionExtensions()
		{
			CardinalDirections = new Direction[] {
				Direction.North,
				Direction.East,
				Direction.South,
				Direction.West,
			};

			IntercardinalDirections = new Direction[] {
				Direction.NorthEast,
				Direction.SouthEast,
				Direction.SouthWest,
				Direction.NorthWest,
			};

			PlanarDirections = new Direction[] {
				Direction.North,
				Direction.NorthEast,
				Direction.East,
				Direction.SouthEast,
				Direction.South,
				Direction.SouthWest,
				Direction.West,
				Direction.NorthWest,
			};

			CardinalUpDownDirections = new Direction[] {
				Direction.North,
				Direction.East,
				Direction.South,
				Direction.West,
				Direction.Up,
				Direction.Down,
			};

			PlanarUpDownDirections = new Direction[] {
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
			};
		}

		public static Direction Reverse(this Direction dir)
		{
			int d = (int)dir;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			x = (((x << 1) - (x >> 1)) ^ 1) - 1;
			y = (((y << 1) - (y >> 1)) ^ 1) - 1;
			z = (((z << 1) - (z >> 1)) ^ 1) - 1;

			d = 0;

			d |= x << DirectionConsts.XShift;
			d |= y << DirectionConsts.YShift;
			d |= z << DirectionConsts.ZShift;

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
			return ((int)dir & (DirectionConsts.Mask << DirectionConsts.ZShift)) == (int)Direction.Up;
		}

		public static bool ContainsDown(this Direction dir)
		{
			return ((int)dir & (DirectionConsts.Mask << DirectionConsts.ZShift)) == (int)Direction.Down;
		}

		public static Direction ToPlanarDirection(this Direction dir)
		{
			int d = (int)dir;
			d &= ~(DirectionConsts.Mask << DirectionConsts.ZShift);
			return (Direction)d;
		}

		public static bool IsValid(this Direction dir)
		{
			int d = (int)dir;

			// Check for extra bits
			if ((d & ~DirectionConsts.FullMask) != 0)
				return false;

			int x = (d >> DirectionConsts.XShift) & DirectionConsts.Mask;
			int y = (d >> DirectionConsts.YShift) & DirectionConsts.Mask;
			int z = (d >> DirectionConsts.ZShift) & DirectionConsts.Mask;

			// 0, 1, 2 are valid values
			return x != 3 && y != 3 && z != 3;
		}
	}
}
