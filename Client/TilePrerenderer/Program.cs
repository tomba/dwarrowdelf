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
			var path = Path.GetDirectoryName(args[0]);
			var tsName = Path.GetFileNameWithoutExtension(args[0]);
			var ts = new TileSetLoader(path, tsName);
			ts.Load();

			var values = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			int numSymbols = (int)values.Max() + 1;

			int[] tileSizes = new int[] {
				8, 10, 12,
				16, 20, 24,
				32, 40, 48,
				64, 80, 96
			};

			int maxTileSize = tileSizes.Max();

			WriteableBitmap target = new WriteableBitmap(tileSizes.Sum(), maxTileSize * numSymbols, 96, 96,
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

					target.WritePixels(new Int32Rect(xOffset, i * maxTileSize, tileSize, tileSize), data, stride, 0);

					xOffset += tileSize;
				}
			}

			target.Unlock();

			string tileSizesStr = string.Join(",", tileSizes.Select(i => i.ToString()));
			var pngEncoder = new PngBitmapEncoder();
			var metadata = new BitmapMetadata("png");
			metadata.SetQuery("/tEXt/Software", "Dwarrowdelf");
			metadata.SetQuery("/tEXt/tilesizes", tileSizesStr);
			var frame = BitmapFrame.Create(target, null, metadata, null);
			pngEncoder.Frames = new BitmapFrame[] { frame };

			using (var stream = File.OpenWrite(args[1]))
				pngEncoder.Save(stream);

			Console.WriteLine("Generate TileSet from {0} to {1}", args[0], args[1]);
		}
	}
}
