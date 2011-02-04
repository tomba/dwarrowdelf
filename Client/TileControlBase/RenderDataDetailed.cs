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
		public RenderTileLayer Floor;
		public RenderTileLayer Interior;
		public RenderTileLayer Object;
		public RenderTileLayer Top;

		uint m_packedValidAndDarkness;



		const int FloorShift = 0;
		const int InteriorShift = 7;
		const int ObjectShift = 14;
		const int TopShift = 21;
		const int ValidShift = 28;

		public byte FloorDarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> FloorShift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << FloorShift); m_packedValidAndDarkness |= (value & 0x7fu) << FloorShift; }
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
