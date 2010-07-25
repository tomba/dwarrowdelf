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
			public double X { get; set; }
			public double Y { get; set; }
			public double Width { get; set; }
			public double Height { get; set; }
			public double DrawingRotation { get; set; }

			public char CharSymbol { get; set; }
			public double CharX { get; set; }
			public double CharY { get; set; }
			public double CharWidth { get; set; }
			public double CharHeight { get; set; }
			public double CharRotation { get; set; }
		}

		IList<SymbolInfo> m_symbolInfoList;
		DrawingCache m_drawingCache;
		Dictionary<SymbolID, Dictionary<GameColor, Drawing>> m_drawingMap = new Dictionary<SymbolID, Dictionary<GameColor, Drawing>>();
		Dictionary<SymbolID, Dictionary<GameColor, Drawing>> m_charDrawingMap = new Dictionary<SymbolID, Dictionary<GameColor, Drawing>>();

		public SymbolDrawingCache(Stream symbolsXmlStream, DrawingCache drawingCache)
		{
			m_drawingCache = drawingCache;
			ParseSymbols(symbolsXmlStream);
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

		public Drawing GetCharDrawing(SymbolID symbolID, GameColor color)
		{
			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_charDrawingMap.TryGetValue(symbolID, out map))
			{
				map = new Dictionary<GameColor, Drawing>();
				m_charDrawingMap[symbolID] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = CreateCharDrawing(symbolID, color);
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
				drawing = m_drawingCache.GetCharacterDrawing(symbol.CharSymbol, color, false).Clone();
				drawing = NormalizeDrawing(drawing, new Point(symbol.CharX, symbol.CharY), new Size(symbol.CharWidth, symbol.CharHeight), symbol.CharRotation, true);
			}
			else
			{
				drawing = m_drawingCache.GetDrawing(symbol.DrawingName, color).Clone();
				drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.Width, symbol.Height), symbol.DrawingRotation, true);
			}

			drawing.Freeze();
			return drawing;
		}

		Drawing CreateCharDrawing(SymbolID symbolID, GameColor color)
		{
			var symbol = m_symbolInfoList.Single(si => si.ID == symbolID);

			var drawing = m_drawingCache.GetCharacterDrawing(symbol.CharSymbol, color, true).Clone();
			drawing = NormalizeDrawing(drawing, new Point(symbol.CharX, symbol.CharY), new Size(symbol.CharWidth, symbol.CharHeight), symbol.CharRotation, false);

			drawing.Freeze();
			return drawing;
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


		void ParseSymbols(Stream symbolsXmlStream)
		{
			XDocument root = XDocument.Load(new StreamReader(symbolsXmlStream));
			XElement rootElem = root.Element("Symbols");

			m_symbolInfoList = new List<SymbolInfo>(rootElem.Elements().Count());
			foreach (XElement elem in rootElem.Elements())
			{
				var symbol = new SymbolInfo();

				symbol.CharX = 0;
				symbol.CharY = 0;
				symbol.CharWidth = 100;
				symbol.CharHeight = 100;

				symbol.X = 0;
				symbol.Y = 0;
				symbol.Width = 100;
				symbol.Height = 100;
				symbol.Name = (string)elem.Element("Name");

				SymbolID id;
				if (Enum.TryParse<SymbolID>(symbol.Name, out id) == false)
					throw new Exception();
				symbol.ID = id;

				if (elem.Element("CharSymbol") != null)
				{
					var charElem = elem.Element("CharSymbol");

					XAttribute attr;

					attr = charElem.Attribute("x");
					if (attr != null)
						symbol.CharX = (double)attr;

					attr = charElem.Attribute("y");
					if (attr != null)
						symbol.CharY = (double)attr;

					attr = charElem.Attribute("w");
					if (attr != null)
						symbol.CharWidth = (double)attr;

					attr = charElem.Attribute("h");
					if (attr != null)
						symbol.CharHeight = (double)attr;

					attr = charElem.Attribute("rotate");
					if (attr != null)
						symbol.CharRotation = (double)attr;

					symbol.CharSymbol = ((string)charElem)[0];
				}

				if (elem.Element("Drawing") != null)
				{
					var drawingElem = elem.Element("Drawing");
					XAttribute attr;

					attr = drawingElem.Attribute("x");
					if (attr != null)
						symbol.X = (double)attr;

					attr = drawingElem.Attribute("y");
					if (attr != null)
						symbol.Y = (double)attr;

					attr = drawingElem.Attribute("w");
					if (attr != null)
						symbol.Width = (double)attr;

					attr = drawingElem.Attribute("h");
					if (attr != null)
						symbol.Height = (double)attr;

					attr = drawingElem.Attribute("rotate");
					if (attr != null)
						symbol.DrawingRotation = (double)attr;

					symbol.DrawingName = (string)drawingElem;
				}
				m_symbolInfoList.Add(symbol);
			}
		}
	}
}