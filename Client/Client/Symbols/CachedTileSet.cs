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
		Dictionary<int, Drawing> m_drawingCache = new Dictionary<int, Drawing>();

		int m_bitmapCacheTileSize;
		Dictionary<int, BitmapSource> m_bitmapCache = new Dictionary<int, BitmapSource>();

		public CachedTileSet(TileSet tileSet)
		{
			m_tileSet = tileSet;
		}

		public string Name { get { return m_tileSet.Name; } }
		public DateTime ModTime { get { return m_tileSet.ModTime; } }

		public Drawing GetDetailedDrawing(SymbolID symbolID, GameColor color)
		{
			int key = ((int)symbolID << 16) | (int)color;

			Drawing drawing;

			if (m_drawingCache.TryGetValue(key, out drawing))
				return drawing;

			drawing = m_tileSet.GetDetailedDrawing(symbolID, color);
			m_drawingCache[key] = drawing;
			return drawing;
		}

		public BitmapSource GetTileBitmap(SymbolID symbolID, GameColor color, int size)
		{
			if (m_bitmapCacheTileSize != size)
			{
				m_bitmapCacheTileSize = size;
				m_bitmapCache = new Dictionary<int, BitmapSource>();
			}

			int key = ((int)symbolID << 16) | (int)color;

			BitmapSource bmp;

			if (m_bitmapCache.TryGetValue(key, out bmp))
				return bmp;

			bmp = m_tileSet.GetTileBitmap(symbolID, color, size);
			m_bitmapCache[key] = bmp;
			return bmp;
		}

		public void GetTileRawBitmap(SymbolID symbolID, int size, byte[] array)
		{
			m_tileSet.GetTileRawBitmap(symbolID, size, array);
		}
	}
}
