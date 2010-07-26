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
	}
}