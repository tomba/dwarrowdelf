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
		/* [ name of the drawing -> [ tile colorization -> drawing ] ] */
		Dictionary<string, Dictionary<GameColor, Drawing>> m_drawingMap;

		Dictionary<char, Dictionary<GameColor, Drawing>> m_charDrawingMap;

		public DrawingCache()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();

			var uri = new Uri("SymbolDrawings.xaml", UriKind.Relative);
			var symbolResources = (ResourceDictionary)Application.LoadComponent(uri);
			m_drawingMap = new Dictionary<string, Dictionary<GameColor, Drawing>>(symbolResources.Count);
			foreach (System.Collections.DictionaryEntry de in symbolResources)
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
				ColorizeDrawing(drawing, color.ToColor());
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
			RGB2HSL(tint, out th, out ts, out tl);

			double ch, cs, cl;
			RGB2HSL(c, out ch, out cs, out cl);

			Color color = HSL2RGB(th, ts, cl);
			color.A = c.A;

			return color;
		}

		// Given H,S,L in range of 0-1
		// Returns a Color (RGB struct) in range of 0-255
		public static Color HSL2RGB(double h, double s, double l)
		{
			double v;
			double r, g, b;

			r = l;   // default to gray
			g = l;
			b = l;
			v = (l <= 0.5) ? (l * (1.0 + s)) : (l + s - l * s);
			if (v > 0)
			{
				double m;
				double sv;
				int sextant;
				double fract, vsf, mid1, mid2;

				m = l + l - v;
				sv = (v - m) / v;
				h *= 6.0;
				if (h >= 6.0)
					h -= 6.0;
				sextant = (int)h;
				fract = h - sextant;
				vsf = v * sv * fract;
				mid1 = m + vsf;
				mid2 = v - vsf;

				switch (sextant)
				{
					case 0:
						r = v;
						g = mid1;
						b = m;
						break;

					case 1:
						r = mid2;
						g = v;
						b = m;
						break;

					case 2:
						r = m;
						g = v;
						b = mid1;
						break;

					case 3:
						r = m;
						g = mid2;
						b = v;
						break;

					case 4:
						r = mid1;
						g = m;
						b = v;
						break;

					case 5:
						r = v;
						g = m;
						b = mid2;
						break;

					default:
						throw new Exception();

				}

			}

			Color rgb = new Color();
			rgb.R = Convert.ToByte(r * 255.0f);
			rgb.G = Convert.ToByte(g * 255.0f);
			rgb.B = Convert.ToByte(b * 255.0f);
			rgb.A = 255;

			return rgb;

		}


		// Given a Color (RGB Struct) in range of 0-255
		// Return H,S,L in range of 0-1
		public static void RGB2HSL(Color rgb, out double h, out double s, out double l)
		{
			double r = rgb.R / 255.0;
			double g = rgb.G / 255.0;
			double b = rgb.B / 255.0;
			double v;
			double m;
			double vm;
			double r2, g2, b2;

			h = 0; // default to black
			s = 0;
			l = 0;
			v = Math.Max(r, g);
			v = Math.Max(v, b);
			m = Math.Min(r, g);
			m = Math.Min(m, b);
			l = (m + v) / 2.0;

			if (l <= 0.0)
				return;

			vm = v - m;
			s = vm;
			if (s > 0.0)
				s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
			else
				return;

			r2 = (v - r) / vm;
			g2 = (v - g) / vm;
			b2 = (v - b) / vm;

			if (r == v)
				h = (g == m ? 5.0 + b2 : 1.0 - g2);
			else if (g == v)
				h = (b == m ? 1.0 + r2 : 3.0 - b2);
			else
				h = (r == m ? 3.0 + g2 : 5.0 - r2);

			h /= 6.0;
		}

		static Drawing CreateCharacterDrawing(char ch, GameColor color, bool fillBg)
		{
			Color c;
			if (color == GameColor.None)
				c = Colors.White;
			else
				c = color.ToColor();

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