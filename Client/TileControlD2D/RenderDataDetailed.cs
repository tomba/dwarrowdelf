using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public struct RenderTileLayer
	{
		public SymbolID SymbolID;
		public GameColor Color;
		public GameColor BgColor;
		public byte DarknessLevel;
	}

	public struct RenderTileDetailed
	{
		public bool IsValid;

		public RenderTileLayer Floor;
		public RenderTileLayer Interior;
		public RenderTileLayer Object;
		public RenderTileLayer Top;
	}
}
