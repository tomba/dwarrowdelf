using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	// Change to enum?
	static class TerrainIDConsts
	{
		public const int SlopeBit = 1 << 7;
		public const int SlopeDirMask = (1 << 7) - 1;
	}

	public enum TerrainID : byte
	{
		Undefined,
		Empty,
		NaturalFloor,
		BuiltFloor,
		NaturalWall,
		Hole,	// used for stairs down

		SlopeNorth = TerrainIDConsts.SlopeBit | Direction.North,
		SlopeNorthEast = TerrainIDConsts.SlopeBit | Direction.NorthEast,
		SlopeEast = TerrainIDConsts.SlopeBit | Direction.East,
		SlopeSouthEast = TerrainIDConsts.SlopeBit | Direction.SouthEast,
		SlopeSouth = TerrainIDConsts.SlopeBit | Direction.South,
		SlopeSouthWest = TerrainIDConsts.SlopeBit | Direction.SouthWest,
		SlopeWest = TerrainIDConsts.SlopeBit | Direction.West,
		SlopeNorthWest = TerrainIDConsts.SlopeBit | Direction.NorthWest,
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
			return ((int)id & TerrainIDConsts.SlopeBit) != 0;
		}

		public static TerrainID ToSlope(this Direction dir)
		{
			return (TerrainID)(TerrainIDConsts.SlopeBit | (int)dir);
		}

		public static Direction ToDir(this TerrainID id)
		{
			return (Direction)((int)id & TerrainIDConsts.SlopeDirMask);
		}

		public static TerrainInfo GetTerrain(TerrainID id)
		{
			return s_terrains[(int)id];
		}
	}
}
