using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace MyGame
{
	class SymbolDrawings
	{
		Drawing[] m_drawings;

		ResourceDictionary m_symbolResources;

		public Drawing[] Drawings { get { return m_drawings; } }

		public SymbolDrawings(IAreaData areaData)
		{
			bool useChars = false;

			var stream = areaData.DrawingStream;

			m_symbolResources = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);

			var symbols = areaData.Symbols;

			m_drawings = new Drawing[symbols.Count];
			for (int i = 0; i < symbols.Count; i++)
			{
				Drawing drawing;
				var symbol = symbols[i];
				if (useChars || symbol.DrawingName == null)
					drawing = CreateCharacterDrawing(symbol.CharSymbol);
				else
					drawing = GetDrawingByName(symbol.DrawingName,
						new Rect(symbol.X, symbol.Y, symbol.Width, symbol.Height));
				m_drawings[i] = drawing;
			}
		}

		Drawing GetDrawingByName(string name, Rect rect)
		{
			Drawing d = ((DrawingBrush)m_symbolResources[name]).Drawing;
			d = NormalizeDrawing(d, rect.Location, rect.Size);
			return d;
		}

		Drawing NormalizeDrawing(Drawing drawing, Point location, Size size)
		{
			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.PushTransform(new TranslateTransform(location.X, location.Y));
				dc.PushTransform(new ScaleTransform(size.Width / drawing.Bounds.Width, 
					size.Height / drawing.Bounds.Height));
				dc.PushTransform(new TranslateTransform(-drawing.Bounds.Left, -drawing.Bounds.Top));
				dc.DrawDrawing(drawing);
				dc.Pop();
				dc.Pop();
				dc.Pop();
			}
			dGroup.Freeze();
			return dGroup;
		}

		Drawing CreateCharacterDrawing(char c)
		{
			DrawingGroup dGroup = new DrawingGroup();

			using (DrawingContext dc = dGroup.Open())
			{
				FormattedText formattedText = new FormattedText(
						c.ToString(),
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						new Typeface("Lucida Console"),
						16,
						Brushes.White);

				// draw black background, for two reasons. First, to cover the terrain below an object,
				// second, to move the drawn text properly. There's probably a better way to do the second
				// one.
				dc.DrawRectangle(Brushes.Black, null, new Rect(new Size(formattedText.Width, formattedText.Height)));
				dc.DrawText(formattedText, new Point(0, 0));
			}

			return NormalizeDrawing(dGroup, new Point(10, 0), new Size(80, 100));
		}

	}
}