using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client.Symbols
{
	public sealed class CachedTileSet : ITileSet
	{
		TileSet m_tileSet;

		public CachedTileSet(TileSet tileSet)
		{
			m_tileSet = tileSet;
		}

		public Drawing GetDetailedDrawing(SymbolID symbolID, GameColor color)
		{
			return m_tileSet.GetDetailedDrawing(symbolID, color);
		}

		public BitmapSource GetTileBitmap(SymbolID symbolID, GameColor color, int size)
		{
			return m_tileSet.GetTileBitmap(symbolID, color, size);
		}

		public byte[] GetTileRawBitmap(SymbolID symbolID, int size)
		{
			return m_tileSet.GetTileRawBitmap(symbolID, size);
		}
	}
}
