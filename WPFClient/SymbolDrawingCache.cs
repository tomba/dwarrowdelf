using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace MyGame.Client
{
	class SymbolDrawingCache
	{
		IList<SymbolInfo> m_symbolInfoList;
		DrawingCache m_drawingCache;
		Dictionary<int, Dictionary<Color, Drawing>> m_drawingMap = new Dictionary<int, Dictionary<Color, Drawing>>();

		bool m_useOnlyChars = false;

		public SymbolDrawingCache(DrawingCache drawingCache, IList<SymbolInfo> symbolInfoList)
		{
			m_drawingCache = drawingCache;
			m_symbolInfoList = symbolInfoList;
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
			var symbol = m_symbolInfoList[symbolID];

			if (m_useOnlyChars || symbol.DrawingName == null)
			{
				var drawing = m_drawingCache.GetCharacterDrawing(symbol.CharSymbol, color, m_useOnlyChars).Clone();
				drawing = NormalizeDrawing(drawing, new Point(10, 0), new Size(80, 100));
				drawing.Freeze();
				return drawing;
			}
			else
			{
				var drawing = m_drawingCache.GetDrawing(symbol.DrawingName, color).Clone();
				drawing = NormalizeDrawing(drawing, new Point(symbol.X, symbol.Y), new Size(symbol.Width, symbol.Height));
				drawing.Freeze();
				return drawing;
			}
		}

		static Drawing NormalizeDrawing(Drawing drawing, Point location, Size size)
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
			return dGroup;
		}
	}
}