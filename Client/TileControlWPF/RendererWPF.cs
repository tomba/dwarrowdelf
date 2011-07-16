using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public class RendererWPF : IRenderer
	{
		ISymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		public RenderData<RenderTileDetailed> RenderData { get; set; }

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;
				m_symbolBitmapCache = null;
			}
		}

		public void Render(DrawingContext dc, Size renderSize, RenderContext ctx)
		{
			dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderSize.Width, renderSize.Height));

			if (this.RenderData == null)
				return;

			if (m_symbolBitmapCache == null)
				m_symbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, (int)ctx.TileSize);

			if (m_symbolBitmapCache.TileSize != (int)ctx.TileSize)
				m_symbolBitmapCache.TileSize = (int)ctx.TileSize;

			var grid = this.RenderData.Grid;

			dc.PushTransform(new TranslateTransform(ctx.RenderOffset.X, ctx.RenderOffset.Y));
			dc.PushTransform(new ScaleTransform(ctx.TileSize, ctx.TileSize));

			for (int y = 0; y < ctx.RenderGridSize.Height && y < this.RenderData.Height; ++y)
			{
				for (int x = 0; x < ctx.RenderGridSize.Width && x < this.RenderData.Width; ++x)
				{
					var rect = new Rect(x, y, 1, 1);
					Render(dc, ref grid[y, x].Terrain, rect);
					Render(dc, ref grid[y, x].Interior, rect);
					Render(dc, ref grid[y, x].Object, rect);
					Render(dc, ref grid[y, x].Top, rect);
				}
			}

			dc.Pop();
			dc.Pop();
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

			var bitmap = m_symbolBitmapCache.GetBitmap(s, layer.Color);
			dc.DrawImage(bitmap, rect);
		}

		public void Dispose()
		{
		}
	}
}
