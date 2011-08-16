using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Dwarrowdelf.Client.TileControl;

namespace AStarTest
{
	class Renderer : ITileRenderer
	{
		RenderView m_renderData;

		public Renderer(RenderView renderData)
		{
			m_renderData = renderData;
		}

		public void Render(DrawingContext dc, Size renderSize, TileRenderContext ctx)
		{
			dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderSize.Width, renderSize.Height));

			dc.PushTransform(new TranslateTransform(ctx.RenderOffset.X, ctx.RenderOffset.Y));
			dc.PushTransform(new ScaleTransform(ctx.TileSize, ctx.TileSize));

			for (int y = 0; y < ctx.RenderGridSize.Height && y < m_renderData.Height; ++y)
			{
				for (int x = 0; x < ctx.RenderGridSize.Width && x < m_renderData.Width; ++x)
				{
					dc.PushTransform(new TranslateTransform(x, y));
					RenderTile(dc, x, y);
					dc.Pop();
				}
			}

			dc.Pop();
			dc.Pop();
		}

		void RenderTile(DrawingContext dc, int x, int y)
		{
			var data = m_renderData.Grid[y, x];
			data.OnRender(dc, true);
		}

		public void Dispose()
		{
		}
	}
}
