using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public abstract class RendererBaseWPF : IRenderer
	{
		ISymbolDrawingCache m_symbolDrawingCache;
		IRenderData m_renderData;

		protected RendererBaseWPF(IRenderData renderData)
		{
			m_renderData = renderData;
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;
				this.SymbolBitmapCache = null;
			}
		}

		protected SymbolBitmapCache SymbolBitmapCache { get; private set; }

		public void Render(DrawingContext dc, Size renderSize, RenderContext ctx)
		{
			dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderSize.Width, renderSize.Height));

			if (this.SymbolBitmapCache == null)
				this.SymbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, (int)ctx.TileSize);

			if (this.SymbolBitmapCache.TileSize != (int)ctx.TileSize)
				this.SymbolBitmapCache.TileSize = (int)ctx.TileSize;

			dc.PushTransform(new TranslateTransform(ctx.RenderOffset.X, ctx.RenderOffset.Y));
			dc.PushTransform(new ScaleTransform(ctx.TileSize, ctx.TileSize));

			for (int y = 0; y < ctx.RenderGridSize.Height && y < m_renderData.Height; ++y)
			{
				for (int x = 0; x < ctx.RenderGridSize.Width && x < m_renderData.Width; ++x)
				{
					RenderTile(dc, x, y);
				}
			}

			dc.Pop();
			dc.Pop();
		}

		protected abstract void RenderTile(DrawingContext dc, int x, int y);

		public void Dispose()
		{
		}
	}
}
