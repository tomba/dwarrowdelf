using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class SymbolDrawingCache : Dwarrowdelf.Client.TileControl.ISymbolDrawingCache
	{
		Symbols.SymbolSet m_symbolSet;
		DrawingCache m_drawingCache;
		Dictionary<SymbolID, Dictionary<GameColor, Drawing>> m_drawingMap;

		public SymbolDrawingCache(Uri symbolInfoUri)
		{
			Load(symbolInfoUri);
		}

		public void Load(Uri symbolInfoUri)
		{
			m_drawingMap = new Dictionary<SymbolID, Dictionary<GameColor, Drawing>>();

			var resInfo = Application.GetRemoteStream(symbolInfoUri);

			var settings = new System.Xaml.XamlXmlReaderSettings()
			{
				LocalAssembly = System.Reflection.Assembly.GetCallingAssembly(),
			};

			using (var reader = new System.Xaml.XamlXmlReader(resInfo.Stream, settings))
				m_symbolSet = (Symbols.SymbolSet)System.Xaml.XamlServices.Load(reader);

			if (m_symbolSet.Drawings != null)
				m_drawingCache = new DrawingCache(new Uri(m_symbolSet.Drawings, UriKind.Relative));
		}

		public Drawing GetDrawing(SymbolID symbolID, GameColor color)
		{
			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_drawingMap.TryGetValue(symbolID, out map))
			{
				map = new Dictionary<GameColor, Drawing>();
				m_drawingMap[symbolID] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = CreateDrawing(symbolID, color);
				map[color] = drawing;
			}

			return drawing;
		}

		Drawing CreateDrawing(SymbolID symbolID, GameColor color)
		{
			var symbol = m_symbolSet.Symbols[symbolID];

			return CreateDrawing(symbol, color);
		}

		Drawing CreateDrawing(Symbols.BaseSymbol symbol, GameColor color)
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

				var fgColor = s.Color.HasValue ? s.Color.Value : color;
				var bgColor = s.Background.HasValue ? s.Background.Value : GameColor.None;

				drawing = DrawCharacter(s.Char, typeface, fontSize, fgColor, bgColor, outline, outlineThickness, s.Reverse);
			}
			else if (symbol is Symbols.DrawingSymbol)
			{
				var s = (Symbols.DrawingSymbol)symbol;
				drawing = m_drawingCache.GetDrawing(s.DrawingName, color).Clone();
			}
			else if (symbol is Symbols.CombinedSymbol)
			{
				var s = (Symbols.CombinedSymbol)symbol;

				var dg = new DrawingGroup();
				using (var dc = dg.Open())
				{
					foreach (Symbols.BaseSymbol bs in s.Symbols)
					{
						var d = CreateDrawing(bs, color);
						dc.DrawDrawing(d);
					}
				}

				drawing = dg;
			}
			else
			{
				throw new Exception();
			}

			drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.W, symbol.H), symbol.Rotate, !symbol.Opaque);

			drawing.Freeze();
			return drawing;
		}

		static Drawing DrawCharacter(char ch, Typeface typeFace, double fontSize, GameColor color, GameColor bgColor,
			bool drawOutline, double outlineThickness, bool reverse)
		{
			Color c;
			if (color == GameColor.None)
				c = Colors.White;
			else
				c = color.ToWindowsColor();

			DrawingGroup dGroup = new DrawingGroup();
			var brush = new SolidColorBrush(c);
			var bgBrush = bgColor != GameColor.None ? new SolidColorBrush(bgColor.ToWindowsColor()) : Brushes.Transparent;
			using (DrawingContext dc = dGroup.Open())
			{
				var formattedText = new FormattedText(
						ch.ToString(),
						System.Globalization.CultureInfo.InvariantCulture,
						FlowDirection.LeftToRight,
						typeFace,
						fontSize, Brushes.Black);


				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));
				var pen = drawOutline ? new Pen(Brushes.Black, outlineThickness) : null;
				var boundingGeometry = new RectangleGeometry(pen != null ? geometry.GetRenderBounds(pen) : geometry.Bounds);

				if (reverse)
					geometry = new CombinedGeometry(GeometryCombineMode.Exclude, boundingGeometry, geometry);

				dc.DrawGeometry(bgBrush, null, boundingGeometry);
				dc.DrawGeometry(brush, pen, geometry);
			}

			return dGroup;
		}

		static Drawing NormalizeDrawing(Drawing drawing, Point location, Size size, double angle, bool bgTransparent)
		{
			var transform = CreateNormalizeTransform(drawing.Bounds, location, size, angle);

			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.DrawRectangle(bgTransparent ? Brushes.Transparent : Brushes.Black, null, new Rect(new Size(100, 100)));

				dc.PushTransform(transform);

				dc.DrawDrawing(drawing);

				dc.Pop();
			}

			return dGroup;
		}

		static Transform CreateNormalizeTransform(Rect bounds, Point location, Size size, double angle)
		{
			var t = new TransformGroup();

			var b = bounds;

			// Move center of the geometry to origin
			t.Children.Add(new TranslateTransform(-b.X - b.Width / 2, -b.Y - b.Height / 2));
			// Rotate around origin
			t.Children.Add(new RotateTransform(angle));

			b = t.TransformBounds(bounds);
			// Scale to requested size
			t.Children.Add(new ScaleTransform(size.Width / b.Width, size.Height / b.Height));

			b = t.TransformBounds(bounds);
			// Move to requested position
			t.Children.Add(new TranslateTransform(-b.X + location.X, -b.Y + location.Y));

			t.Freeze();

			b = t.TransformBounds(bounds);
			Debug.Assert(Math.Abs(b.X - location.X) < 0.0001);
			Debug.Assert(Math.Abs(b.Y - location.Y) < 0.0001);
			Debug.Assert(Math.Abs(b.Width - size.Width) < 0.0001);
			Debug.Assert(Math.Abs(b.Height - size.Height) < 0.0001);

			return t;
		}
	}
}