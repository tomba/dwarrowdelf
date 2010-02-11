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
		/* color black == no colorization */
		Dictionary<string, Dictionary<Color, Drawing>> m_drawingMap;

		Dictionary<char, Dictionary<Color, Drawing>> m_charDrawingMap;

		public DrawingCache()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();

			var uri = new Uri("PlanetCute.xaml", UriKind.Relative);
			var symbolResources = (ResourceDictionary)Application.LoadComponent(uri);
			m_drawingMap = new Dictionary<string, Dictionary<Color, Drawing>>(symbolResources.Count);
			foreach (System.Collections.DictionaryEntry de in symbolResources)
			{
				Drawing drawing = ((DrawingBrush)de.Value).Drawing;
				string name = (string)de.Key;
				m_drawingMap[name] = new Dictionary<Color, Drawing>();
				m_drawingMap[name][Colors.Black] = drawing;
			}

			m_charDrawingMap = new Dictionary<char, Dictionary<Color, Drawing>>();
		}

		public Drawing GetDrawing(string drawingName, Color color)
		{
			Dictionary<Color, Drawing> map;
			Drawing drawing;

			if (!m_drawingMap.TryGetValue(drawingName, out map))
				return null;

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = m_drawingMap[drawingName][Colors.Black].Clone();
				ColorizeDrawing(drawing, color);
				drawing.Freeze();
				map[color] = drawing;
			}

			return drawing;
		}

		public Drawing GetCharacterDrawing(char character, Color color, bool fillBg)
		{
			Dictionary<Color, Drawing> map;
			Drawing drawing;

			if (!m_charDrawingMap.TryGetValue(character, out map))
			{
				map = new Dictionary<Color, Drawing>();
				m_charDrawingMap[character] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = CreateCharacterDrawing(character, color, fillBg).Clone();
				ColorizeDrawing(drawing, color);
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

		// r,g,b values are from 0 to 1
		// h = [0,360], s = [0,1], v = [0,1]
		//		if s == 0, then h = -1 (undefined)
		static void RGBtoHSV(float r, float g, float b, out float h, out float s, out float v)
		{
			float min, max, delta;

			min = Math.Min(r, Math.Min(g, b));
			max = Math.Max(r, Math.Max(g, b));
			v = max;				// v

			delta = max - min;

			if (max != 0)
				s = delta / max;		// s
			else
			{
				// r = g = b = 0		// s = 0, v is undefined
				s = 0;
				h = -1;
				return;
			}

			if (r == max)
				h = (g - b) / delta;		// between yellow & magenta
			else if (g == max)
				h = 2 + (b - r) / delta;	// between cyan & yellow
			else
				h = 4 + (r - g) / delta;	// between magenta & cyan

			h *= 60;				// degrees
			if (h < 0)
				h += 360;

		}

		static void HSVtoRGB(out float r, out float g, out float b, float h, float s, float v)
		{
			int i;
			float f, p, q, t;

			if (s == 0)
			{
				// achromatic (grey)
				r = g = b = v;
				return;
			}

			h /= 60;			// sector 0 to 5
			i = (int)Math.Floor(h);
			f = h - i;			// factorial part of h
			p = v * (1 - s);
			q = v * (1 - s * f);
			t = v * (1 - s * (1 - f));

			switch (i)
			{
				case 0:
					r = v;
					g = t;
					b = p;
					break;
				case 1:
					r = q;
					g = v;
					b = p;
					break;
				case 2:
					r = p;
					g = v;
					b = t;
					break;
				case 3:
					r = p;
					g = q;
					b = v;
					break;
				case 4:
					r = t;
					g = p;
					b = v;
					break;
				default:		// case 5:
					r = v;
					g = p;
					b = q;
					break;
			}

		}

		static Color TintColor(Color c, Color tint)
		{
			byte r = c.R;
			byte g = c.G;
			byte b = c.B;

			float tint_hue;
			float h, s, v;
			float fr, fg, fb;

			byte rr = tint.R;
			byte gg = tint.G;
			byte bb = tint.B;
			RGBtoHSV((float)rr / 255.0f, (float)gg / 255.0f, (float)bb / 255.0f, out h, out s, out v);

			tint_hue = h;

			RGBtoHSV((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, out h, out s, out v);
			HSVtoRGB(out fr, out fg, out fb, tint_hue, s, v);
			r = (byte)(fr * 255.0f);
			g = (byte)(fg * 255.0f);
			b = (byte)(fb * 255.0f);

			return Color.FromArgb(c.A, r, g, b);
		}

		static Drawing CreateCharacterDrawing(char c, Color color, bool fillBg)
		{
			if (color == Colors.Black)
				color = Colors.White;

			DrawingGroup dGroup = new DrawingGroup();
			Brush brush = new SolidColorBrush(color);
			using (DrawingContext dc = dGroup.Open())
			{
				var typeFace = new Typeface(new FontFamily("Lucida Console"),
					FontStyles.Normal,
					FontWeights.Bold,
					FontStretches.Normal);

				var formattedText = new FormattedText(
						c.ToString(),
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						typeFace,
						16,	Brushes.Black);

				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));

				var bg = fillBg ? Brushes.Black : Brushes.Transparent;
				var pen = fillBg ? null : new Pen(Brushes.Black, 0.5);
				dc.DrawRectangle(bg, null, new Rect(new Size(formattedText.Width, formattedText.Height)));
				dc.DrawGeometry(brush, pen, geometry);
			}

			return dGroup;
		}
	}
}