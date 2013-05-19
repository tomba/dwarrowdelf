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
		public BitmapSource Atlas { get; private set; }
		public int MaxTileSize { get; private set; }
		Dictionary<int, int> m_tileSizeMap;
		LRUCache<TileKey, BitmapSource> m_cache = new LRUCache<TileKey, BitmapSource>(128);

		public TileSet(Uri uri)
		{
			var resStream = Application.GetResourceStream(uri);

			string tileSizesStr;

			using (var stream = resStream.Stream)
			{
				var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
				var frame = decoder.Frames[0];
				var meta = (BitmapMetadata)frame.Metadata;
				tileSizesStr = (string)meta.GetQuery("/tEXt/tilesizes");

				this.Atlas = new WriteableBitmap(frame);
				this.Atlas.Freeze();
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

		public BitmapSource GetTile(SymbolID symbolID, GameColor color, int tileSize)
		{
			var key = new TileKey(symbolID, color, tileSize);

			BitmapSource bmp;

			if (m_cache.TryGet(key, out bmp))
				return bmp;

			int xOffset = GetTileXOffset(tileSize);
			int yOffset = GetTileYOffset(symbolID);

			bmp = new CroppedBitmap(this.Atlas, new Int32Rect(xOffset, yOffset, tileSize, tileSize));

			if (color != GameColor.None)
				bmp = ColorizeBitmap(bmp, color.ToWindowsColor());

			bmp.Freeze();

			m_cache.Add(key, bmp);

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

		struct TileKey : IEquatable<TileKey>
		{
			public SymbolID SymbolID;
			public GameColor Color;
			public int Size;

			public TileKey(SymbolID symbolID, GameColor color, int size)
			{
				this.SymbolID = symbolID;
				this.Color = color;
				this.Size = size;
			}

			public bool Equals(TileKey other)
			{
				return this.SymbolID == other.SymbolID && this.Color == other.Color && this.Size == other.Size;
			}

			public override bool Equals(object obj)
			{
				return (obj is TileKey) && Equals((TileKey)obj);
			}

			public override int GetHashCode()
			{
				return this.Size | ((int)this.Color << 8) | ((int)this.SymbolID << 16);
			}
		}
	}
}
