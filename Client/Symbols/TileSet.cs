using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace Dwarrowdelf.Client.Symbols
{
	public sealed class TileSet : ITileSet
	{
		static TileSet s_defaultTileSet;

		static TileSet()
		{
			s_defaultTileSet = new TileSet("DefaultTileSet");
			s_defaultTileSet.Load();
		}

		public static TileSet Default { get { return s_defaultTileSet; } }

		SymbolSet m_symbolSet;
		Dictionary<string, Drawing> m_drawingResources;
		BitmapSource m_bitmap;
		int[] m_bitmapSizes;

		public string Name { get; private set; }

		public TileSet(string symbolInfoName)
		{
			this.Name = symbolInfoName;
			m_symbolSet = LoadSymbolSet(this.Name + ".xaml");
		}

		public void Load()
		{
			m_drawingResources = LoadDrawingResource(this.Name + "Vectors.xaml");

			m_bitmap = LoadBitmapResource(this.Name + "Bitmaps.png");

			m_bitmapSizes = m_symbolSet.BitmapSizes.Split(',').Select(s => int.Parse(s)).ToArray();
		}

		static SymbolSet LoadSymbolSet(string symbolInfoName)
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			var path = Path.Combine(Path.GetDirectoryName(asm.Location), "Symbols", symbolInfoName);

			SymbolSet symbolSet;

			if (File.Exists(path))
			{
				using (var stream = File.OpenRead(path))
					symbolSet = (Symbols.SymbolSet)System.Xaml.XamlServices.Load(stream);
			}
			else
			{
				var uri = new Uri("/Dwarrowdelf.Client.Symbols;component/" + symbolInfoName, UriKind.Relative);
				symbolSet = (Symbols.SymbolSet)Application.LoadComponent(uri);
			}

			return symbolSet;
		}

		static Dictionary<string, Drawing> LoadDrawingResource(string drawingsName)
		{
			ResourceDictionary drawingResources;

			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			var path = Path.Combine(Path.GetDirectoryName(asm.Location), "Symbols", drawingsName);

			if (File.Exists(path))
			{
				using (var stream = File.OpenRead(path))
					drawingResources = (ResourceDictionary)System.Xaml.XamlServices.Load(stream);
			}
			else
			{
				try
				{
					var uri = new Uri("/Dwarrowdelf.Client.Symbols;component/" + drawingsName, UriKind.Relative);
					drawingResources = (ResourceDictionary)Application.LoadComponent(uri);
				}
				catch (IOException)
				{
					return null;
				}
			}

			var drawingMap = new Dictionary<string, Drawing>(drawingResources.Count);

			foreach (System.Collections.DictionaryEntry de in drawingResources)
			{
				Drawing drawing = ((DrawingBrush)de.Value).Drawing;
				drawing.Freeze();
				string name = (string)de.Key;
				drawingMap[name] = drawing;
			}

			return drawingMap;
		}

		static BitmapFrame LoadBitmapResource(string name)
		{
			Stream stream;

			try
			{
				var uri = new Uri("/Dwarrowdelf.Client.Symbols;component/" + name, UriKind.Relative);
				var resStream = Application.GetResourceStream(uri);
				stream = resStream.Stream;
			}
			catch (IOException)
			{
				return null;
			}

			var decoder = PngBitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
			return decoder.Frames[0];
		}


		static uint ParseSizes(string sizeStr)
		{
			if (sizeStr == null)
				return 0;

			uint mask = 0;

			foreach (var str in sizeStr.Split(','))
			{
				uint s = uint.Parse(str);

				if (IsPowerOfTwo(s) == false)
					throw new Exception();

				mask |= s;
			}

			return mask;
		}

		static bool IsPowerOfTwo(uint x)
		{
			return (x & (x - 1)) == 0;
		}

		public Drawing GetDetailedDrawing(SymbolID symbolID, GameColor color)
		{
			if (m_symbolSet.Symbols.Contains(symbolID) == false)
				symbolID = SymbolID.Unknown;

			var symbol = m_symbolSet.Symbols[symbolID];

			var gfx = DecideGfx(symbol);

			if (gfx is VectorGfxBase)
			{
				var g = (VectorGfxBase)gfx;

				var drawing = GetVectorGfx(g);

				if (color != GameColor.None)
				{
					drawing = drawing.Clone();
					TileSetHelpers.ColorizeDrawing(drawing, color.ToWindowsColor());
				}

				drawing.Freeze();

				return drawing;
			}
			else if (gfx is BitmapGfx)
			{
				var g = (BitmapGfx)gfx;

				var bmp = GetBitmapGfx(g, 64);

				if (color != GameColor.None)
					bmp = TileSetHelpers.ColorizeBitmap(bmp, color.ToWindowsColor());

				var drawing = TileSetHelpers.BitmapToDrawing(bmp);

				drawing.Freeze();

				return drawing;
			}
			else
			{
				throw new Exception();
			}
		}

		GfxBase DecideGfx(Symbol symbol)
		{
			var gfxs = symbol.Graphics;

			if (gfxs.Count == 1)
				return gfxs[0];

			GfxBase gfx;

			gfx = gfxs.OfType<VectorGfxBase>().FirstOrDefault();
			if (gfx != null)
				return gfx;

			gfx = gfxs.OfType<BitmapGfx>().FirstOrDefault();
			if (gfx != null)
				return gfx;

			throw new Exception();
		}

		public BitmapSource GetTileBitmap(SymbolID symbolID, GameColor color, int size)
		{
			if (m_symbolSet.Symbols.Contains(symbolID) == false)
				symbolID = SymbolID.Unknown;

			var symbol = m_symbolSet.Symbols[symbolID];

			var gfx = DecideTileGfx(symbol, size);

			if (gfx is VectorGfxBase)
			{
				var g = (VectorGfxBase)gfx;

				var drawing = GetVectorGfx(g);

				var bmp = TileSetHelpers.DrawingToBitmap(drawing, size);

				if (color != GameColor.None)
					bmp = TileSetHelpers.ColorizeBitmap(bmp, color.ToWindowsColor());

				return bmp;
			}
			else if (gfx is BitmapGfx)
			{
				var g = (BitmapGfx)gfx;
				var bmp = GetBitmapGfx(g, size);

				if (color != GameColor.None)
					bmp = TileSetHelpers.ColorizeBitmap(bmp, color.ToWindowsColor());

				return bmp;
			}
			else
			{
				throw new Exception();
			}
		}

		public void GetTileRawBitmap(SymbolID symbolID, int size, byte[] array)
		{
			var bmp = GetTileBitmap(symbolID, GameColor.None, size);

			const int bytesPerPixel = 4;
			bmp.CopyPixels(array, size * bytesPerPixel, 0);
		}

		GfxBase DecideTileGfx(Symbol symbol, int size)
		{
			var gfxs = symbol.Graphics;

			if (gfxs.Count == 1)
				return gfxs[0];

			GfxBase gfx;

			gfx = gfxs.OfType<BitmapGfx>().FirstOrDefault();
			if (gfx != null)
			{
				if (size <= m_bitmapSizes.Max())
					return gfx;
			}

			gfx = gfxs.OfType<VectorGfxBase>().FirstOrDefault();
			if (gfx != null)
				return gfx;

			throw new Exception();
		}

		Drawing GetVectorGfx(VectorGfxBase gfx)
		{
			if (gfx is CharGfx)
			{
				var g = (CharGfx)gfx;
				var drawing = CreateCharDrawing(g);
				return drawing;
			}
			else if (gfx is DrawingGfx)
			{
				var g = (DrawingGfx)gfx;
				var drawing = CreateVectorDrawing(g);
				return drawing;
			}
			else if (gfx is CombinedGfx)
			{
				var g = (CombinedGfx)gfx;

				var dg = new DrawingGroup();
				using (var dc = dg.Open())
				{
					foreach (var bs in g.Symbols)
					{
						var t = GetVectorGfx(bs);
						dc.DrawDrawing(t);
					}
				}

				return dg;
			}
			else
			{
				throw new Exception();
			}
		}

		Drawing CreateCharDrawing(CharGfx gfx)
		{
			var fontFamily = gfx.FontFamily != null ? gfx.FontFamily : m_symbolSet.FontFamily;
			var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
			var fontSize = gfx.FontSize.HasValue ? gfx.FontSize.Value : m_symbolSet.FontSize;
			var outline = gfx.Outline.HasValue ? gfx.Outline.Value : m_symbolSet.Outline;
			var outlineThickness = gfx.OutlineThickness.HasValue ? gfx.OutlineThickness.Value : m_symbolSet.OutlineThickness;

			var fgColor = gfx.Color.HasValue ? gfx.Color.Value : GameColor.White;
			var bgColor = gfx.Background.HasValue ? gfx.Background.Value : GameColor.None;

			var drawing = TileSetHelpers.DrawCharacter(gfx.Char, typeface, fontSize, fgColor, bgColor, outline,
				outlineThickness, gfx.Reverse, gfx.Mode);

			drawing = TileSetHelpers.NormalizeDrawing(drawing, new Point(gfx.X, gfx.Y), new Size(gfx.W, gfx.H),
				gfx.Rotate, !gfx.Opaque, gfx.Opacity);

			drawing.Freeze();

			return drawing;
		}

		Drawing CreateVectorDrawing(DrawingGfx gfx)
		{
			var drawing = m_drawingResources[gfx.DrawingName].Clone();

			drawing = TileSetHelpers.NormalizeDrawing(drawing, new Point(gfx.X, gfx.Y), new Size(gfx.W, gfx.H),
				gfx.Rotate, !gfx.Opaque, gfx.Opacity);

			RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.Fant);

			drawing.Freeze();

			return drawing;
		}

		BitmapSource GetBitmapGfx(BitmapGfx gfx, int size)
		{
			int xoff;
			int tileSize = 0;

			for (int i = 0; i < m_bitmapSizes.Length; ++i)
			{
				if (size <= m_bitmapSizes[i])
				{
					tileSize = m_bitmapSizes[i];
					break;
				}
			}

			if (tileSize == 0)
				tileSize = m_bitmapSizes.Max();

			xoff = 1;

			for (int i = 0; i < m_bitmapSizes.Length; ++i)
			{
				if (tileSize == m_bitmapSizes[i])
					break;

				xoff += m_bitmapSizes[i] + 3;
			}

			BitmapSource bmp = new CroppedBitmap(m_bitmap, new Int32Rect(xoff, 1 + 35 * gfx.BitmapIndex, tileSize, tileSize));

			if (size != tileSize)
			{
				double scale = (double)size / tileSize;
				bmp = new TransformedBitmap(bmp, new ScaleTransform(scale, scale));
			}

			return bmp;
		}
	}
}
