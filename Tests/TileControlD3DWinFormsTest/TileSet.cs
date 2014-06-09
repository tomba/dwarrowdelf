using Dwarrowdelf.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileControlD3DWinFormsTest
{
	public class TileSet : Dwarrowdelf.Client.TileControl.ITileSet
	{
		public int MaxTileSize { get; private set; }
		Dictionary<int, int> m_tileSizeMap;
		byte[] m_rawBitmap;
		int m_rawBitmapWidth;

		public TileSet(string filename)
		{
			string tileSizesStr = "8, 10, 12,16, 20, 24,32, 40, 48,64, 80, 96";

			using (var bmp = (Bitmap)Bitmap.FromFile(filename))
			{
				m_rawBitmapWidth = bmp.Width;

				// Lock the bitmap's bits.  
				Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
				System.Drawing.Imaging.BitmapData bmpData =
					bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
					bmp.PixelFormat);

				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// Declare an array to hold the bytes of the bitmap. 
				int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
				m_rawBitmap = new byte[bytes];

				// Copy the RGB values into the array.
				System.Runtime.InteropServices.Marshal.Copy(ptr, m_rawBitmap, 0, bytes);
			}

			var tileSizes = tileSizesStr.Split(',').Select(s => int.Parse(s)).ToArray();

			MaxTileSize = tileSizes.Max();

			m_tileSizeMap = new Dictionary<int, int>(tileSizes.Length);

			int xOffset = 0;
			foreach (var tileSize in tileSizes)
			{
				m_tileSizeMap[tileSize] = xOffset;
				xOffset += tileSize;
			}
		}

		public byte[] GetRawBitmap()
		{
			return m_rawBitmap;
		}

		public int RawBitmapWidth { get { return m_rawBitmapWidth; } }

		public bool HasTileSize(int tileSize)
		{
			return m_tileSizeMap.ContainsKey(tileSize);
		}

		public int GetTileXOffset(int tileSize)
		{
			int xOffset;

			if (m_tileSizeMap.TryGetValue(tileSize, out xOffset) == false)
				throw new Exception();

			return xOffset;
		}

		public int GetTileYOffset(SymbolID symbolID)
		{
			return (int)symbolID * MaxTileSize;
		}
	}
}
