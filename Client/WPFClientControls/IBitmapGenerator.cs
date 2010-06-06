using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MyGame.Client
{
	public interface IBitmapGenerator
	{
		BitmapSource GetBitmap(SymbolID symbolID, Color color, bool dark);
		int NumDistinctBitmaps { get; }
		int TileSize { get; set; }
	}
}
