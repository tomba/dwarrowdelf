using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControl
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
