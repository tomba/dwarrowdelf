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
	public sealed class VectorTileSet : ITileSet
	{
		Dictionary<SymbolID, Drawing> m_baseDrawingMap;
		Dictionary<SymbolID, Dictionary<GameColor, Drawing>> m_drawingMap;

		public VectorTileSet(string symbolInfoName)
		{
			var loader = new DrawingLoader(symbolInfoName);
			m_baseDrawingMap = loader.Drawings;

			m_drawingMap = new Dictionary<SymbolID, Dictionary<GameColor, Drawing>>();
		}

		public Drawing GetDrawing(SymbolID symbolID, GameColor color)
		{
			if (color == GameColor.None)
				return m_baseDrawingMap[symbolID];

			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_drawingMap.TryGetValue(symbolID, out map))
			{
				map = new Dictionary<GameColor, Drawing>();
				m_drawingMap[symbolID] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = m_baseDrawingMap[symbolID].Clone();
				TileSetHelpers.ColorizeDrawing(drawing, color.ToWindowsColor());
				drawing.Freeze();
				map[color] = drawing;
			}

			return drawing;
		}

		public BitmapSource GetBitmap(SymbolID symbolID, GameColor color, int size)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			var d = GetDrawing(symbolID, color);

			drawingContext.PushTransform(new ScaleTransform((double)size / 100, (double)size / 100));
			drawingContext.DrawDrawing(d);
			drawingContext.Pop();
			drawingContext.Close();

			RenderTargetBitmap bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();

			return bmp;
		}
	}

	class DrawingLoader
	{
		static SymbolSet s_charSymbolSet;

		static DrawingLoader()
		{
			var uri = new Uri("/Dwarrowdelf.Client.Symbols;component/SymbolInfosChar.xaml", UriKind.Relative);
			s_charSymbolSet = (Symbols.SymbolSet)Application.LoadComponent(uri);
		}


		SymbolSet m_symbolSet;
		Dictionary<string, Drawing> m_drawingResources;

		public DrawingLoader(string symbolInfoName)
		{
			m_symbolSet = LoadSymbolSet(symbolInfoName);

			if (m_symbolSet.Drawings != null)
				m_drawingResources = LoadDrawingResources(m_symbolSet.Drawings);
			else
				m_drawingResources = new Dictionary<string, Drawing>();

			var drawingMap = new Dictionary<SymbolID, Drawing>();

			foreach (var symbolID in EnumHelpers.GetEnumValues<SymbolID>())
			{
				drawingMap[symbolID] = CreateDrawing(symbolID);
			}

			this.Drawings = drawingMap;
		}

		public Dictionary<SymbolID, Drawing> Drawings { get; private set; }

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

		static Dictionary<string, Drawing> LoadDrawingResources(string drawingsName)
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
				var uri = new Uri("/Dwarrowdelf.Client.Symbols;component/" + drawingsName, UriKind.Relative);
				drawingResources = (ResourceDictionary)Application.LoadComponent(uri);
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

		Drawing CreateDrawing(SymbolID symbolID)
		{
			Symbols.BaseSymbol symbol;

			if (m_symbolSet.Symbols.Contains(symbolID))
				symbol = m_symbolSet.Symbols[symbolID];
			else
				symbol = s_charSymbolSet.Symbols[symbolID];

			return CreateDrawing(symbol);
		}

		Drawing CreateDrawing(Symbols.BaseSymbol symbol)
		{
			Drawing drawing;

			if (symbol is Symbols.CharSymbol)
			{
				var s = (Symbols.CharSymbol)symbol;
				var fontFamily = s.FontFamily != null ? s.FontFamily : m_symbolSet.FontFamily;
				var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
				var fontSize = s.FontSize.HasValue ? s.FontSize.Value : m_symbolSet.FontSize;
				var outline = s.Outline.HasValue ? s.Outline.Value : m_symbolSet.Outline;
				var outlineThickness = s.OutlineThickness.HasValue ? s.OutlineThickness.Value : m_symbolSet.OutlineThickness;

				var fgColor = s.Color.HasValue ? s.Color.Value : GameColor.White;
				var bgColor = s.Background.HasValue ? s.Background.Value : GameColor.None;

				drawing = TileSetHelpers.DrawCharacter(s.Char, typeface, fontSize, fgColor, bgColor, outline, outlineThickness, s.Reverse);
			}
			else if (symbol is Symbols.DrawingSymbol)
			{
				var s = (Symbols.DrawingSymbol)symbol;
				drawing = m_drawingResources[s.DrawingName].Clone();
				RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.Fant);
			}
			else if (symbol is Symbols.CombinedSymbol)
			{
				var s = (Symbols.CombinedSymbol)symbol;

				var dg = new DrawingGroup();
				using (var dc = dg.Open())
				{
					foreach (Symbols.BaseSymbol bs in s.Symbols)
					{
						var d = CreateDrawing(bs);
						dc.DrawDrawing(d);
					}
				}

				drawing = dg;
			}
			else
			{
				throw new Exception();
			}

			drawing = TileSetHelpers.NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.W, symbol.H),
				symbol.Rotate, !symbol.Opaque, symbol.Opacity);

			drawing.Freeze();
			return drawing;
		}
	}
}
