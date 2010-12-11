using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	enum FloorIDConsts
	{
		SlopeBit = 1 << 7,
		SlopeDirMask = (1 << 7) - 1,
	}

	public enum FloorID : byte
	{
		Undefined,
		Empty,
		NaturalFloor,
		Hole,	// used for stairs down

		SlopeNorth = FloorIDConsts.SlopeBit | Direction.North,
		SlopeNorthEast = FloorIDConsts.SlopeBit | Direction.NorthEast,
		SlopeEast = FloorIDConsts.SlopeBit | Direction.East,
		SlopeSouthEast = FloorIDConsts.SlopeBit | Direction.SouthEast,
		SlopeSouth = FloorIDConsts.SlopeBit | Direction.South,
		SlopeSouthWest = FloorIDConsts.SlopeBit | Direction.SouthWest,
		SlopeWest = FloorIDConsts.SlopeBit | Direction.West,
		SlopeNorthWest = FloorIDConsts.SlopeBit | Direction.NorthWest,
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
			return ((int)id & (int)FloorIDConsts.SlopeBit) != 0;
		}

		public static FloorID ToSlope(this Direction dir)
		{
			return (FloorID)((int)FloorIDConsts.SlopeBit | (int)dir);
		}

		public static Direction ToDir(this FloorID id)
		{
			return (Direction)((int)id & (int)FloorIDConsts.SlopeDirMask);
		}

		public static FloorInfo GetFloor(FloorID id)
		{
			return s_floorList[(int)id];
		}

		public static readonly FloorInfo Undefined = new FloorInfo(FloorID.Undefined, 0);
		public static readonly FloorInfo Empty = new FloorInfo(FloorID.Empty, 0);
		public static readonly FloorInfo Floor = new FloorInfo(FloorID.NaturalFloor, FloorFlags.Blocking | FloorFlags.Carrying);

		public static readonly FloorInfo SlopeNorth = new FloorInfo(FloorID.SlopeNorth, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeNorthEast = new FloorInfo(FloorID.SlopeNorthEast, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeEast = new FloorInfo(FloorID.SlopeEast, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeSouthEast = new FloorInfo(FloorID.SlopeSouthEast, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeSouth = new FloorInfo(FloorID.SlopeSouth, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeSouthWest = new FloorInfo(FloorID.SlopeSouthWest, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeWest = new FloorInfo(FloorID.SlopeWest, FloorFlags.Blocking | FloorFlags.Carrying);
		public static readonly FloorInfo SlopeNorthWest = new FloorInfo(FloorID.SlopeNorthWest, FloorFlags.Blocking | FloorFlags.Carrying);

		public static readonly FloorInfo Hole = new FloorInfo(FloorID.Hole, FloorFlags.Carrying);
	}
}
