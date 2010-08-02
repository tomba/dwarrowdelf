using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Xml.Linq;

namespace MyGame.Client
{
	class SymbolDrawingCache
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
			var reader = new System.Windows.Markup.XamlReader();
			m_symbolSet = (Symbols.SymbolSet)reader.LoadAsync(resInfo.Stream);

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
			Drawing drawing;

			if (symbol is Symbols.CharSymbol)
			{
				var s = (Symbols.CharSymbol)symbol;
				var typeface = s.Typeface != null ? s.Typeface : m_symbolSet.Typeface;
				var outline = s.Outline.HasValue ? s.Outline.Value : m_symbolSet.Outline;
				drawing = DrawCharacter(s.Char, typeface, color, outline);
			}
			else if (symbol is Symbols.DrawingSymbol)
			{
				var s = (Symbols.DrawingSymbol)symbol;
				drawing = m_drawingCache.GetDrawing(s.DrawingName, color).Clone();
			}
			else
			{
				throw new Exception();
			}

			drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.W, symbol.H), symbol.Rotate, !symbol.Opaque);

			drawing.Freeze();
			return drawing;
		}

		static Drawing DrawCharacter(char ch, Typeface typeFace, GameColor color, bool drawOutline)
		{
			Color c;
			if (color == GameColor.None)
				c = Colors.White;
			else
				c = color.ToWindowsColor();

			DrawingGroup dGroup = new DrawingGroup();
			Brush brush = new SolidColorBrush(c);
			using (DrawingContext dc = dGroup.Open())
			{
				var formattedText = new FormattedText(
						ch.ToString(),
						System.Globalization.CultureInfo.InvariantCulture,
						FlowDirection.LeftToRight,
						typeFace,
						16, Brushes.Black);

				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));

				var pen = drawOutline ? new Pen(Brushes.Black, 0.5) : null;
				dc.DrawGeometry(brush, pen, geometry);
			}

			return dGroup;
		}

		static Drawing NormalizeDrawing(Drawing drawing, Point location, Size size, double angle, bool bgTransparent)
		{
			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.DrawRectangle(bgTransparent ? Brushes.Transparent : Brushes.Black, null, new Rect(new Size(100, 100)));

				dc.PushTransform(new RotateTransform(angle, 50, 50));
				dc.PushTransform(new TranslateTransform(location.X, location.Y));
				dc.PushTransform(new ScaleTransform(size.Width / drawing.Bounds.Width, size.Height / drawing.Bounds.Height));
				dc.PushTransform(new TranslateTransform(-drawing.Bounds.Left, -drawing.Bounds.Top));

				dc.DrawDrawing(drawing);

				dc.Pop();
				dc.Pop();
				dc.Pop();
				dc.Pop();
			}

			return dGroup;
		}
	}
}