using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client
{
	public class TileSet
	{
		BitmapSource m_atlas;
		Dictionary<int, int> m_tileSizeMap;
		int m_maxTileSize;

		public BitmapSource Atlas { get { return m_atlas; } }

		public TileSet(Uri uri)
		{
			var resStream = Application.GetResourceStream(uri);

			using (var stream = resStream.Stream)
			{
				var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
				var frame = decoder.Frames[0];
				m_atlas = new WriteableBitmap(frame);
				m_atlas.Freeze();
			}

			m_maxTileSize = 64;

			m_tileSizeMap = new Dictionary<int, int>(4);
			m_tileSizeMap[8] = 0;
			m_tileSizeMap[16] = 8;
			m_tileSizeMap[32] = 8 + 16;
			m_tileSizeMap[64] = 8 + 16 + 32;
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
			return (int)symbolID * m_maxTileSize;
		}

		public BitmapSource GetTile(SymbolID symbolID, GameColor color, int tileSize)
		{
			if (tileSize == 24)
				tileSize = 32;

			int xOffset = GetTileXOffset(tileSize);
			int yOffset = GetTileYOffset(symbolID);

			BitmapSource bmp = new CroppedBitmap(m_atlas, new Int32Rect(xOffset, yOffset, tileSize, tileSize));

			if (color != GameColor.None)
				bmp = ColorizeBitmap(bmp, color.ToWindowsColor());

			bmp.Freeze();

			return bmp;
		}

		static Color TintColor(Color c, Color tint)
		{
			return SimpleTint(c, tint);
#if asd
			double th, ts, tl;
			HSL.RGB2HSL(tint, out th, out ts, out tl);

			double ch, cs, cl;
			HSL.RGB2HSL(c, out ch, out cs, out cl);

			Color color = HSL.HSL2RGB(th, ts, cl);
			color.A = c.A;

			return color;
#endif
		}

		static Color SimpleTint(Color c, Color tint)
		{
			return Color.FromScRgb(
				c.ScA * tint.ScA,
				c.ScR * tint.ScR,
				c.ScG * tint.ScG,
				c.ScB * tint.ScB);
		}

		static BitmapSource ColorizeBitmap(BitmapSource bmp, Color tint)
		{
			var wbmp = new WriteableBitmap(bmp);
			var arr = new uint[wbmp.PixelWidth * wbmp.PixelHeight];

			wbmp.CopyPixels(arr, wbmp.PixelWidth * 4, 0);

			for (int i = 0; i < arr.Length; ++i)
			{
				byte a = (byte)((arr[i] >> 24) & 0xff);
				byte r = (byte)((arr[i] >> 16) & 0xff);
				byte g = (byte)((arr[i] >> 8) & 0xff);
				byte b = (byte)((arr[i] >> 0) & 0xff);

				var c = Color.FromArgb(a, r, g, b);
				c = TintColor(c, tint);

				arr[i] = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | (c.B << 0));
			}

			wbmp.WritePixels(new Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight), arr, wbmp.PixelWidth * 4, 0);

			return wbmp;
		}
	}
}
