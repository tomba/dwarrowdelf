using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	class FloorIDConsts
	{
		public const int SlopeBit = 1 << 7;
		public const int SlopeDirMask = (1 << 7) - 1;
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
		public FloorID ID { get; set; }
		public string Name { get; set; }
		public FloorFlags Flags { get; set; }

		public bool IsCarrying { get { return (this.Flags & FloorFlags.Carrying) != 0; } }
		public bool IsBlocking { get { return (this.Flags & FloorFlags.Blocking) != 0; } }

		public bool IsSeeThrough { get { return IsBlocking; } }
		public bool IsWaterPassable { get { return !IsBlocking; } }
	}

	public static class Floors
	{
		static FloorInfo[] s_floors;

		static Floors()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			FloorInfo[] floors;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Floors.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					floors = (FloorInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = floors.Max(m => (int)m.ID);
			s_floors = new FloorInfo[max + 1];

			foreach (var item in floors)
			{
				if (s_floors[(int)item.ID] != null)
					throw new Exception("Duplicate entry");

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				s_floors[(int)item.ID] = item;
			}

			s_floors[0] = new FloorInfo()
			{
				ID = FloorID.Undefined,
				Name = "<undefined>",
			};
		}

		public static bool IsSlope(this FloorID id)
		{
			return ((int)id & FloorIDConsts.SlopeBit) != 0;
		}

		public static FloorID ToSlope(this Direction dir)
		{
			return (FloorID)(FloorIDConsts.SlopeBit | (int)dir);
		}

		public static Direction ToDir(this FloorID id)
		{
			return (Direction)((int)id & FloorIDConsts.SlopeDirMask);
		}

		public static FloorInfo GetFloor(FloorID id)
		{
			return s_floors[(int)id];
		}
	}
}
