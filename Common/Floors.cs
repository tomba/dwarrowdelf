using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum FloorID : byte
	{
		Undefined,
		Empty,
		NaturalFloor,
		Hole,	// used for stairs down
		SlopeNorth,
		SlopeSouth,
		SlopeWest,
		SlopeEast,
	}

	[Flags]
	public enum FloorFlags
	{
		None = 0,
		Blocking = 1 << 0,	/* dwarf can not go through it */
		Carrying = 1 << 1,	/* dwarf can stand over it */
	}

	public class FloorInfo
	{
		public FloorInfo(FloorID id, FloorFlags flags)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.Flags = flags;
		}

		public FloorID ID { get; private set; }
		public string Name { get; private set; }
		public FloorFlags Flags { get; private set; }

		public bool IsCarrying { get { return (this.Flags & FloorFlags.Carrying) != 0; } }
		public bool IsBlocking { get { return (this.Flags & FloorFlags.Blocking) != 0; } }

		public bool IsSeeThrough { get { return IsBlocking; } }
		public bool IsWaterPassable { get { return !IsBlocking; } }
	}

	public static class Floors
	{
		static FloorInfo[] s_floorList;

		static Floors()
		{
			var arr = (FloorID[])Enum.GetValues(typeof(FloorID));
			var max = arr.Max();
			s_floorList = new FloorInfo[(int)max + 1];

			foreach (var field in typeof(Floors).GetFields())
			{
				if (field.FieldType != typeof(FloorInfo))
					continue;

				var floorInfo = (FloorInfo)field.GetValue(null);
				s_floorList[(int)floorInfo.ID] = floorInfo;
			}
		}

		public static bool IsSlope(this FloorID id)
		{
			return id == FloorID.SlopeNorth || id == FloorID.SlopeSouth || id == FloorID.SlopeEast || id == FloorID.SlopeWest;
		}

		public static FloorID ToSlope(this Direction dir)
		{
			switch (dir)
			{
				case Direction.North:
					return FloorID.SlopeNorth;
				case Direction.East:
					return FloorID.SlopeEast;
				case Direction.South:
					return FloorID.SlopeSouth;
				case Direction.West:
					return FloorID.SlopeWest;
				default:
					throw new Exception();
			}
		}

		public static Direction ToDir(this FloorID id)
		{
			switch (id)
			{
				case FloorID.SlopeNorth:
					return Direction.North;
				case FloorID.SlopeEast:
					return Direction.East;
				case FloorID.SlopeSouth:
					return Direction.South;
				case FloorID.SlopeWest:
					return Direction.West;
				default:
					throw new Exception();
			}
		}

		public static FloorInfo GetFloor(FloorID id)
		{
			return s_floorList[(int)id];
		}

		public static readonly FloorInfo Undefined = new FloorInfo(FloorID.Undefined, 0);
		public static readonly FloorInfo Empty = new FloorInfo(FloorID.Empty, 0);
		public static readonly FloorInfo Floor = new FloorInfo(FloorID.NaturalFloor, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeNorth = new FloorInfo(FloorID.SlopeNorth, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeSouth = new FloorInfo(FloorID.SlopeSouth, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeWest = new FloorInfo(FloorID.SlopeWest, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeEast = new FloorInfo(FloorID.SlopeEast, FloorFlags.Blocking | FloorFlags.Carrying);

		public static readonly FloorInfo Hole = new FloorInfo(FloorID.Hole, FloorFlags.Carrying);
	}
}
