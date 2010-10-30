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

	public enum Positioning
	{
		Exact,
		AdjacentCardinal,
		AdjacentPlanar,
		AdjacentCardinalUpDown,
		AdjacentPlanarUpDown,
		Adjacent,
	}

	public static class DirectionExtensions
	{
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
	}
}
