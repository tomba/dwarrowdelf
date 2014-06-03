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
	public struct RenderTile
	{
		public RenderTileLayer Layer0;
		public RenderTileLayer Layer1;
		public RenderTileLayer Layer2;
		public RenderTileLayer Layer3;

		uint m_packedValidAndDarkness;



		const int Layer0Shift = 0;
		const int Layer1Shift = 7;
		const int Layer2Shift = 14;
		const int Layer3Shift = 21;
		const int ValidShift = 28;

		public byte Layer0DarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> Layer0Shift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << Layer0Shift); m_packedValidAndDarkness |= (value & 0x7fu) << Layer0Shift; }
		}

		public byte Layer1DarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> Layer1Shift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << Layer1Shift); m_packedValidAndDarkness |= (value & 0x7fu) << Layer1Shift; }
		}

		public byte Layer2DarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> Layer2Shift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << Layer2Shift); m_packedValidAndDarkness |= (value & 0x7fu) << Layer2Shift; }
		}

		public byte Layer3DarknessLevel
		{
			get { return (byte)((m_packedValidAndDarkness >> Layer3Shift) & 0x7fu); }
			set { m_packedValidAndDarkness &= ~(0x7fu << Layer3Shift); m_packedValidAndDarkness |= (value & 0x7fu) << Layer3Shift; }
		}

		public bool IsValid
		{
			get { return ((m_packedValidAndDarkness >> ValidShift) & 1u) != 0; }
			set { m_packedValidAndDarkness &= ~(1u << ValidShift); m_packedValidAndDarkness |= (value ? 1u : 0u) << ValidShift; }
		}
	}
}
