using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyGame.Client
{
	struct RenderTile
	{
		public bool IsValid;
		// Tile color for minimap
		//public GameColor Color;

		public SymbolID FloorSymbolID;
		public bool FloorDark;

		public SymbolID InteriorSymbolID;
		public bool InteriorDark;

		public SymbolID ObjectSymbolID;
		public GameColor ObjectColor;
		public bool ObjectDark;

		public SymbolID TopSymbolID;
		public bool TopDark;
	}

	class RenderMap
	{
		ArrayGrid2D<RenderTile> m_grid;

		public RenderMap()
		{
			m_grid = new ArrayGrid2D<RenderTile>(0, 0);
		}

		public IntSize Size
		{
			get { return m_grid.Size; }
			set { m_grid = new ArrayGrid2D<RenderTile>(value); }
		}

		public ArrayGrid2D<RenderTile> ArrayGrid { get { return m_grid; } }

		public void Clear()
		{
			m_grid.Clear();
		}
	}
}
