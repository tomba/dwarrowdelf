using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace MyGame
{
	class SymbolDrawingCache
	{
		IList<SymbolInfo> m_symbolInfoList;
		Dictionary<string, Drawing> m_resourceDrawingMap;

		Dictionary<int, Dictionary<Color, Drawing>> m_drawingMap =
			new Dictionary<int, Dictionary<Color, Drawing>>();

		bool m_useOnlyChars = false;

		public SymbolDrawingCache(IAreaData areaData)
		{
			m_symbolInfoList = areaData.Symbols;

			if (!m_useOnlyChars)
			{
				var symbolResources = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(areaData.DrawingStream);
				m_resourceDrawingMap = new Dictionary<string, Drawing>(symbolResources.Count);
				foreach (System.Collections.DictionaryEntry a in symbolResources)
				{
					Drawing drawing = ((DrawingBrush)a.Value).Drawing;
					string name = (string)a.Key;

					SymbolInfo si = m_symbolInfoList.Single(s => s.DrawingName == name);
					drawing = NormalizeDrawing(drawing, new Point(si.X, si.Y), new Size(si.Width, si.Height));
					m_resourceDrawingMap[name] = drawing;
				}
			}
		}

		public Drawing GetDrawing(int symbolID, Color color)
		{
			Dictionary<Color, Drawing> map;
			Drawing drawing;
			
			if (!m_drawingMap.TryGetValue(symbolID, out map))
			{
				map = new Dictionary<Color, Drawing>();
				m_drawingMap[symbolID] = map;
			}

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = CreateDrawing(symbolID, color);
				map[color] = drawing;
			}

			return drawing;
		}

		Drawing CreateDrawing(int symbolID, Color color)
		{
			Drawing drawing;
			var symbol = m_symbolInfoList[symbolID];

			if (m_useOnlyChars || symbol.DrawingName == null)
			{
				return CreateCharacterDrawing(symbol.CharSymbol, color);
			}
			else
			{
				drawing = m_resourceDrawingMap[symbol.DrawingName];
				
				if (color == Colors.Black)
					return drawing;

				// aarga this is horrible!
				string xamlstr = System.Windows.Markup.XamlWriter.Save(drawing);
				var regex = new System.Text.RegularExpressions.Regex("((Brush)|(Color))=\"#(........)\"");
				xamlstr = regex.Replace(xamlstr, m => TintReplacer(m, color));
				drawing = (Drawing)System.Windows.Markup.XamlReader.Parse(xamlstr);

				return drawing;
			}
		}

		string TintReplacer(System.Text.RegularExpressions.Match m, Color tint)
		{
			uint val = uint.Parse(m.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
			byte a = (byte)((val >> 24) & 0xff);
			byte r = (byte)((val >> 16) & 0xff);
			byte g = (byte)((val >> 8) & 0xff);
			byte b = (byte)((val >> 0) & 0xff);

			var c = Color.FromArgb(a, r, g, b);

			//float t = 0.2f;
			//c = Color.Multiply(c, t) + Color.Multiply(tint, 1 - t);

			c = TintColor(c, tint);

			return String.Format("{0}=\"{1}\"", m.Groups[1].Value, c);
		}

		// r,g,b values are from 0 to 1
		// h = [0,360], s = [0,1], v = [0,1]
		//		if s == 0, then h = -1 (undefined)

		void RGBtoHSV(float r, float g, float b, out float h, out float s, out float v)
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

		void HSVtoRGB(out float r, out float g, out float b, float h, float s, float v)
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

		Color TintColor(Color c, Color tint)
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

		Drawing CreateCharacterDrawing(char c, Color color)
		{
			if (color == Colors.Black)
				color = Colors.White;

			DrawingGroup dGroup = new DrawingGroup();
			Brush brush = new SolidColorBrush(color);
			using (DrawingContext dc = dGroup.Open())
			{
				FormattedText formattedText = new FormattedText(
						c.ToString(),
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						new Typeface("Lucida Console"),
						16,	brush);

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