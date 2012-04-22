using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public abstract class RendererBaseWPF : ISymbolTileRenderer
	{
		ITileSet m_tileSet;
		IRenderData m_renderData;

		protected RendererBaseWPF(IRenderData renderData)
		{
			m_renderData = renderData;
		}

		public ITileSet TileSet
		{
			get { return m_tileSet; }

			set
			{
				m_tileSet = value;
			}
		}

		void OnDrawingsChanged()
		{
		}

		public void Render(DrawingContext dc, Size renderSize, TileRenderContext ctx)
		{
			dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderSize.Width, renderSize.Height));

			dc.PushTransform(new TranslateTransform(ctx.RenderOffset.X, ctx.RenderOffset.Y));
			dc.PushTransform(new ScaleTransform(ctx.TileSize, ctx.TileSize));

			int size = (int)ctx.TileSize;

			for (int y = 0; y < ctx.RenderGridSize.Height && y < m_renderData.Height; ++y)
			{
				for (int x = 0; x < ctx.RenderGridSize.Width && x < m_renderData.Width; ++x)
				{
					RenderTile(dc, x, y, size);
				}
			}

			dc.Pop();
			dc.Pop();
		}

		protected abstract void RenderTile(DrawingContext dc, int x, int y, int size);

		public void Dispose()
		{
		}
	}
}
