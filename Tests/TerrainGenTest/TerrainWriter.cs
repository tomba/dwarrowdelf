using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf;
using System.Windows;
using System.Windows.Media;

namespace TerrainGenTest
{
	class TerrainWriter
	{
		public static WriteableBitmap Do()
		{
			int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp);

			var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);

			var gen = new TerrainGen(sizeExp, 10, 5, 0.75);

			var diff = gen.Max - gen.Min;
			var mul = 255 / diff;

			uint[] array = new uint[size];

			bmp.Lock();

			for (int y = 0; y < size; ++y)
			{
					if (y >= bmp.PixelHeight)
						continue;

				for (int x = 0; x < size; ++x)
				{
					if (x >= bmp.PixelWidth)
						continue;
		
					var v = gen.Grid[new IntPoint(x, y)];

					v -= gen.Min;
					v *= mul;

					if (v < 0 || v > 255)
						throw new Exception();

					var r = (uint)v;
					var g = (uint)v;
					var b = (uint)v;

					array[x] = (r << 16) | (g << 8) | (b << 0);
				}

				bmp.WritePixels(new Int32Rect(0, y, size, 1), array, size * 4, 0);
			}

			bmp.AddDirtyRect(new Int32Rect(0, 0, size, size));
			bmp.Unlock();
			return bmp;
		}
	}
}
