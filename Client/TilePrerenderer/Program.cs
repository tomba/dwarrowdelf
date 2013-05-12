using Dwarrowdelf;
using Dwarrowdelf.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TilePrerenderer
{
	class Program
	{
		static void Main(string[] args)
		{
			var ts = new TileSetLoader("DefaultTileSet");
			ts.Load();
			var n = ts.Name;

			var values = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			int numSymbols = (int)values.Max() + 1;

			int[] tileSizes = new int[] { 8, 16, 32, 64 };

			WriteableBitmap target = new WriteableBitmap(tileSizes.Sum(), tileSizes.Max() * numSymbols, 96, 96,
				PixelFormats.Bgra32, null);

			target.Lock();

			var buf = target.BackBuffer;

			// leave the first one (Undefined) empty
			for (int i = 1; i < numSymbols; ++i)
			{
				int xOffset = 0;

				foreach (int tileSize in tileSizes)
				{
					var source = ts.GetTileBitmap((SymbolID)i, tileSize);

					int stride = source.PixelWidth * (source.Format.BitsPerPixel / 8);
					byte[] data = new byte[stride * source.PixelHeight];

					source.CopyPixels(data, stride, 0);

					target.WritePixels(new Int32Rect(xOffset, i * 64, tileSize, tileSize), data, stride, 0);

					xOffset += tileSize;
				}
			}

			target.Unlock();

			var p = new PngBitmapEncoder();
			p.Frames = new BitmapFrame[] { BitmapFrame.Create(target) };
			p.Save(File.OpenWrite("c:/temp/test.png"));
		}
	}
}
