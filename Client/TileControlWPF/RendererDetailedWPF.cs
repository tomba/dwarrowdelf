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

		protected override void RenderTile(DrawingContext dc, int x, int y, int size)
		{
			var rect = new Rect(x, y, 1, 1);
			var grid = m_renderData.Grid;
			int idx = m_renderData.GetIdx(x, y);
			Render(dc, ref grid[idx].Terrain, rect, size);
			Render(dc, ref grid[idx].Interior, rect, size);
			Render(dc, ref grid[idx].Object, rect, size);
			Render(dc, ref grid[idx].Top, rect, size);
		}

		void Render(DrawingContext dc, ref RenderTileLayer layer, Rect rect, int size)
		{
			var s = layer.SymbolID;

			if (s == SymbolID.Undefined)
				return;

			if (layer.BgColor != GameColor.None)
			{
				var rgb = layer.BgColor.ToGameColorRGB();
				dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B)), null, rect);
			}

			var bitmap = this.TileSet.GetBitmap(s, layer.Color, size);
			dc.DrawImage(bitmap, rect);
		}
	}
}
