using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client
{
	public interface ITileSet
	{
		Drawing GetDetailedDrawing(SymbolID symbolID, GameColor color);
		BitmapSource GetTileBitmap(SymbolID symbolID, GameColor color, int size);
		byte[] GetTileRawBitmap(SymbolID symbolID, int size);
	}
}
