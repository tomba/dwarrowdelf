using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererDetailedWPF : RendererBaseWPF
	{
		RenderData<RenderTileDetailed> m_renderData;

		public RendererDetailedWPF(RenderData<RenderTileDetailed> renderData)
			: base(renderData)
		{
			m_renderData = renderData;
		}

		protected override void RenderTile(DrawingContext dc, int x, int y)
		{
			var rect = new Rect(x, y, 1, 1);
			var grid = m_renderData.Grid;
			Render(dc, ref grid[y, x].Terrain, rect);
			Render(dc, ref grid[y, x].Interior, rect);
			Render(dc, ref grid[y, x].Object, rect);
			Render(dc, ref grid[y, x].Top, rect);
		}

		void Render(DrawingContext dc, ref RenderTileLayer layer, Rect rect)
		{
			var s = layer.SymbolID;

			if (s == SymbolID.Undefined)
				return;

			if (layer.BgColor != GameColor.None)
			{
				var rgb = layer.BgColor.ToGameColorRGB();
				dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B)), null, rect);
			}

			var bitmap = this.SymbolBitmapCache.GetBitmap(s, layer.Color);
			dc.DrawImage(bitmap, rect);
		}
	}
}
