using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Resources;

namespace MyGame.Client
{
	class DrawingCache
	{
		/* [ name of the drawing -> [ color -> drawing ] ] */
		Dictionary<string, Dictionary<GameColor, Drawing>> m_drawingMap;

		/* [ character -> [ color -> drawing ] ] */
		Dictionary<char, Dictionary<GameColor, Drawing>> m_charDrawingMap;

		public DrawingCache(ResourceDictionary drawingResources)
		{
			m_drawingMap = new Dictionary<string, Dictionary<GameColor, Drawing>>(drawingResources.Count);

			foreach (System.Collections.DictionaryEntry de in drawingResources)
			{
				Drawing drawing = ((DrawingBrush)de.Value).Drawing;
				string name = (string)de.Key;
				m_drawingMap[name] = new Dictionary<GameColor, Drawing>();
				m_drawingMap[name][GameColor.None] = drawing;
			}

			m_charDrawingMap = new Dictionary<char, Dictionary<GameColor, Drawing>>();
		}

		public Drawing GetDrawing(string drawingName, GameColor color)
		{
			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_drawingMap.TryGetValue(drawingName, out map))
				return null;

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = m_drawingMap[drawingName][GameColor.None].Clone();
				ColorizeDrawing(drawing, color.ToWindowsColor());
				drawing.Freeze();
				map[color] = drawing;
			}

			return drawing;
		}

		public Drawing GetCharacterDrawing(char character, GameColor color, bool fillBg)
		{
			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_charDrawingMap.TryGetValue(character, out map))
			{
				map = new Dictionary<GameColor, Drawing>();
				m_charDrawingMap[character] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = CreateCharacterDrawing(character, color, fillBg).Clone();
				drawing.Freeze();
				map[color] = drawing;
			}

			return drawing;
		}


		static void ColorizeDrawing(Drawing drawing, Color tintColor)
		{
			if (drawing is DrawingGroup)
			{
				var dg = (DrawingGroup)drawing;
				foreach (var d in dg.Children)
				{
					ColorizeDrawing(d, tintColor);
				}
			}
			else if (drawing is GeometryDrawing)
			{
				var gd = (GeometryDrawing)drawing;
				if (gd.Brush != null)
					ColorizeBrush(gd.Brush, tintColor);
				if (gd.Pen != null)
					ColorizeBrush(gd.Pen.Brush, tintColor);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		static void ColorizeBrush(Brush brush, Color tintColor)
		{
			if (brush is SolidColorBrush)
			{
				var b = (SolidColorBrush)brush;
				b.Color = TintColor(b.Color, tintColor);
			}
			else if (brush is LinearGradientBrush)
			{
				var b = (LinearGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = TintColor(stop.Color, tintColor);
			}
			else if (brush is RadialGradientBrush)
			{
				var b = (RadialGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = TintColor(stop.Color, tintColor);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		static Color TintColor(Color c, Color tint)
		{
			double th, ts, tl;
			HSL.RGB2HSL(tint, out th, out ts, out tl);

			double ch, cs, cl;
			HSL.RGB2HSL(c, out ch, out cs, out cl);

			Color color = HSL.HSL2RGB(th, ts, cl);
			color.A = c.A;

			return color;
		}

		static Drawing CreateCharacterDrawing(char ch, GameColor color, bool fillBg)
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
				var typeFace = new Typeface(new FontFamily("Lucida Console"),
					FontStyles.Normal,
					FontWeights.Normal,
					FontStretches.Normal);

				var formattedText = new FormattedText(
						ch.ToString(),
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						typeFace,
						16, Brushes.Black);

				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));

				var bg = fillBg ? Brushes.Black : Brushes.Transparent;
				var pen = fillBg ? null : new Pen(Brushes.Black, 0.5);
				dc.DrawGeometry(brush, pen, geometry);
			}

			return dGroup;
		}
	}
}