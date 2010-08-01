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
		class SymbolInfo
		{
			public SymbolID ID { get; set; }
			public string Name { get; set; }

			public string DrawingName { get; set; }
			public char CharSymbol { get; set; }

			public double X { get; set; }
			public double Y { get; set; }
			public double Width { get; set; }
			public double Height { get; set; }
			public double Rotation { get; set; }
			public Typeface Typeface { get; set; }
			public bool Outline { get; set; }
		}

		IList<SymbolInfo> m_symbolInfoList;
		DrawingCache m_drawingCache;
		Dictionary<SymbolID, Dictionary<GameColor, Drawing>> m_drawingMap;

		public SymbolDrawingCache(Uri symbolInfoUri)
		{
			Load(symbolInfoUri);
		}

		public void Load(Uri symbolInfoUri)
		{
			string drawingsName;

			m_drawingMap = new Dictionary<SymbolID, Dictionary<GameColor, Drawing>>();

			ParseSymbols(symbolInfoUri, out drawingsName);

			if (drawingsName != null)
				m_drawingCache = new DrawingCache(new Uri(drawingsName, UriKind.Relative));
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
			var symbol = m_symbolInfoList.Single(si => si.ID == symbolID);
			Drawing drawing;

			if (symbol.DrawingName == null)
			{
				drawing = DrawCharacter(symbol.CharSymbol, symbol.Typeface, color, symbol.Outline);
				drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.Width, symbol.Height), symbol.Rotation, true);
			}
			else
			{
				drawing = m_drawingCache.GetDrawing(symbol.DrawingName, color).Clone();
				drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.Width, symbol.Height), symbol.Rotation, true);
			}

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

		void ParseSymbols(Uri uri, out string drawingsName)
		{
			var resInfo = Application.GetRemoteStream(uri);

			Stream symbolsXmlStream = resInfo.Stream;

			XDocument doc = XDocument.Load(new StreamReader(symbolsXmlStream));
			var symbolDefs = doc.Element("SymbolSet");

			if (symbolDefs.Element("Drawings") != null)
				drawingsName = (string)symbolDefs.Element("Drawings");
			else
				drawingsName = null;

			var fontName = (string)symbolDefs.Element("Font");

			bool outline = false;
			if (symbolDefs.Element("Outline") != null)
				outline = (bool)symbolDefs.Element("Outline");

			var defaultTypeFace = new Typeface(new FontFamily(fontName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

			var symbols = symbolDefs.Element("Symbols");

			m_symbolInfoList = new List<SymbolInfo>(symbols.Elements().Count());
			foreach (XElement elem in symbols.Elements())
			{
				var symbol = new SymbolInfo();

				symbol.X = 0;
				symbol.Y = 0;
				symbol.Width = 100;
				symbol.Height = 100;
				symbol.Name = (string)elem.Element("Name");

				SymbolID id;
				if (Enum.TryParse<SymbolID>(symbol.Name, out id) == false)
					throw new Exception();
				symbol.ID = id;

				XElement e;
				XAttribute attr;

				if (elem.Element("Char") != null)
				{
					e = elem.Element("Char");
					symbol.CharSymbol = ((string)e)[0];

					attr = e.Attribute("font");
					if (attr != null)
						symbol.Typeface = new Typeface(new FontFamily((string)attr), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
					else
						symbol.Typeface = defaultTypeFace;

					symbol.Outline = outline;
				}
				else
				{
					e = elem.Element("Drawing");
					symbol.DrawingName = (string)e;
				}

				attr = e.Attribute("x");
				if (attr != null)
					symbol.X = (double)attr;

				attr = e.Attribute("y");
				if (attr != null)
					symbol.Y = (double)attr;

				attr = e.Attribute("w");
				if (attr != null)
					symbol.Width = (double)attr;

				attr = e.Attribute("h");
				if (attr != null)
					symbol.Height = (double)attr;

				attr = e.Attribute("rotate");
				if (attr != null)
					symbol.Rotation = (double)attr;

				m_symbolInfoList.Add(symbol);
			}
		}
	}
}