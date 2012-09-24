using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum TerrainID : byte
	{
		Undefined = 0,
		Empty,
		NaturalFloor,
		BuiltFloor,
		NaturalWall,
		StairsDown,

		SlopeNorth,
		SlopeNorthEast,
		SlopeEast,
		SlopeSouthEast,
		SlopeSouth,
		SlopeSouthWest,
		SlopeWest,
		SlopeNorthWest,
	}

	[Flags]
	public enum TerrainFlags
	{
		None = 0,
		Blocker = 1 << 1,		// The tile can not be entered
		Minable = 1 << 2,		// can be mined, changing the terrain to floor
		Permeable = 1 << 3,		// Fluids can pass through
		Supporting = 1 << 4,	// supports standing upon
		BlocksVision = 1 << 5,	// Blocks line of sight
		SeeThroughDown = 1 << 6,// The tile below is visible
	}

	public sealed class TerrainInfo
	{
		public TerrainID ID { get; internal set; }
		public string Name { get; internal set; }
		public TerrainFlags Flags { get; internal set; }

		/// <summary>
		/// Tile can not be entered
		/// </summary>
		public bool IsBlocker { get { return (this.Flags & TerrainFlags.Blocker) != 0; } }

		/// <summary>
		/// Tile can be stood upon
		/// </summary>
		public bool IsSupporting { get { return (this.Flags & TerrainFlags.Supporting) != 0; } }

		/// <summary>
		/// Fluids can flow downwards through the tile
		/// </summary>
		public bool IsPermeable { get { return (this.Flags & TerrainFlags.Permeable) != 0; } }

		/// <summary>
		/// Tile can be mined, becoming a floor
		/// </summary>
		public bool IsMinable { get { return (this.Flags & TerrainFlags.Minable) != 0; } }

		/// <summary>
		/// Does not block line of sight in planar directions
		/// </summary>
		public bool IsSeeThrough { get { return (this.Flags & TerrainFlags.BlocksVision) == 0; } }

		public bool IsSeeThroughDown { get { return (this.Flags & TerrainFlags.SeeThroughDown) != 0; } }
	}

	public static class Terrains
	{
		static TerrainInfo[] s_terrains;

		static Terrains()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			TerrainInfo[] terrains;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Terrains.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					terrains = (TerrainInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = terrains.Max(m => (int)m.ID);
			s_terrains = new TerrainInfo[max + 1];

			foreach (var item in terrains)
			{
				if (s_terrains[(int)item.ID] != null)
					throw new Exception("Duplicate entry");

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				s_terrains[(int)item.ID] = item;
			}

			s_terrains[0] = new TerrainInfo()
			{
				ID = TerrainID.Undefined,
				Name = "<undefined>",
			};
		}

		public static bool IsSlope(this TerrainID id)
		{
			switch (id)
			{
				case TerrainID.SlopeNorth:
				case TerrainID.SlopeNorthEast:
				case TerrainID.SlopeEast:
				case TerrainID.SlopeSouthEast:
				case TerrainID.SlopeSouth:
				case TerrainID.SlopeSouthWest:
				case TerrainID.SlopeWest:
				case TerrainID.SlopeNorthWest:
					return true;
			}

			return false;
		}

		public static TerrainID ToSlope(this Direction dir)
		{
			switch (dir)
			{
				case Direction.North:
					return TerrainID.SlopeNorth;
				case Direction.NorthEast:
					return TerrainID.SlopeNorthEast;
				case Direction.East:
					return TerrainID.SlopeEast;
				case Direction.SouthEast:
					return TerrainID.SlopeSouthEast;
				case Direction.South:
					return TerrainID.SlopeSouth;
				case Direction.SouthWest:
					return TerrainID.SlopeSouthWest;
				case Direction.West:
					return TerrainID.SlopeWest;
				case Direction.NorthWest:
					return TerrainID.SlopeNorthWest;
				default:
					throw new Exception();
			}
		}

		public static Direction ToDir(this TerrainID id)
		{
			switch (id)
			{
				case TerrainID.SlopeNorth:
					return Direction.North;
				case TerrainID.SlopeNorthEast:
					return Direction.NorthEast;
				case TerrainID.SlopeEast:
					return Direction.East;
				case TerrainID.SlopeSouthEast:
					return Direction.SouthEast;
				case TerrainID.SlopeSouth:
					return Direction.South;
				case TerrainID.SlopeSouthWest:
					return Direction.SouthWest;
				case TerrainID.SlopeWest:
					return Direction.West;
				case TerrainID.SlopeNorthWest:
					return Direction.NorthWest;
				default:
					throw new Exception();
			}
		}

		public static bool IsFloor(this TerrainID id)
		{
			return id == TerrainID.NaturalFloor || id == TerrainID.BuiltFloor;
		}

		public static TerrainInfo GetTerrain(TerrainID id)
		{
			return s_terrains[(int)id];
		}
	}
}
