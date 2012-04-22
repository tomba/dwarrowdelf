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
		Drawing GetDrawing(SymbolID symbolID, GameColor color);
		BitmapSource GetBitmap(SymbolID symbolID, GameColor color, int size);
	}
}
