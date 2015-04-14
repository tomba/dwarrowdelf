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
		StairsDown,
		Slope,
	}

	[Flags]
	enum TerrainFlags
	{
		None = 0,
		Blocker = 1 << 1,		// The tile can not be entered
		Permeable = 1 << 3,		// Fluids can pass through
		Supporting = 1 << 4,	// supports standing upon
		BlocksVision = 1 << 5,	// Blocks line of sight
		SeeThroughDown = 1 << 6,// The tile below is visible
	}

	sealed class TerrainInfo
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
		/// Does not block line of sight in planar directions
		/// </summary>
		public bool IsSeeThrough { get { return (this.Flags & TerrainFlags.BlocksVision) == 0; } }

		public bool IsSeeThroughDown { get { return (this.Flags & TerrainFlags.SeeThroughDown) != 0; } }
	}

	static class Terrains
	{
		static TerrainInfo[] s_terrains;

		static Terrains()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			TerrainInfo[] terrains;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Game.Terrains.xaml"))
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

		public static TerrainInfo GetTerrain(TerrainID id)
		{
			return s_terrains[(int)id];
		}
	}

	public static class TerrainExtensions
	{
		public static bool IsSlope(this TerrainID id)
		{
			return id == TerrainID.Slope;
		}

		public static bool IsFloor(this TerrainID id)
		{
			return id == TerrainID.NaturalFloor || id == TerrainID.BuiltFloor;
		}
	}
}
