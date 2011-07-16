using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Dwarrowdelf.Client.TileControl
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RenderTileLayer
	{
		public SymbolID SymbolID;
		public GameColor Color;
		public GameColor BgColor;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct RenderTileDetailed
	{
		public RenderTileLayer Terrain;
		public RenderTileLayer Interior;
		public RenderTileLayer Object;
		public RenderTileLayer Top;

		uint m_packedValidAndDarkness;



		const int TerrainShift = 0;
		const int InteriorShift = 7;
		const int ObjectShift = 14;
		const int TopShift = 21;
		const int ValidShift = 28;

		public byte TerrainDarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> TerrainShift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << TerrainShift); m_packedValidAndDarkness |= (value & 0x7fu) << TerrainShift; }
		}

		public byte InteriorDarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> InteriorShift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << InteriorShift); m_packedValidAndDarkness |= (value & 0x7fu) << InteriorShift; }
		}

		public byte ObjectDarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> ObjectShift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << ObjectShift); m_packedValidAndDarkness |= (value & 0x7fu) << ObjectShift; }
		}

		public byte TopDarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> TopShift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << TopShift); m_packedValidAndDarkness |= (value & 0x7fu) << TopShift; }
		}

		public bool IsValid
		{
			get { return ((m_packedValidAndDarkness >> ValidShift) & 1u) != 0; }
			set { m_packedValidAndDarkness &= ~(1u << ValidShift); m_packedValidAndDarkness |= (value ? 1u : 0u) << ValidShift; }
		}
	}
}
