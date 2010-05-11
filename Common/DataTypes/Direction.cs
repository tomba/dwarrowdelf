using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyGame
{
	public static class DirectionConsts
	{
		public const int Mask = 0x3;

		public const int XShift = 0;
		public const int YShift = 2;
		public const int ZShift = 4;

		public const int DirNone = 0;
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

	public static class DirectionExtensions
	{
		public static readonly ReadOnlyCollection<Direction> CardinalDirections;
		public static readonly ReadOnlyCollection<Direction> IntercardinalDirections;
		public static readonly ReadOnlyCollection<Direction> PlanarDirections;

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
		}

		public static Direction Reverse(this Direction dir)
		{
			// XXX optimize
			return new IntVector3D(dir).Reverse().ToDirection();
		}

		public static bool IsCardinal(this Direction dir)
		{
			return dir == Direction.North || dir == Direction.East || dir == Direction.South || dir == Direction.West;
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
