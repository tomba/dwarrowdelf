using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using Dwarrowdelf;

namespace AStarTest
{
	class Renderer
	{
		static Pen s_edgePen;

		static Renderer()
		{
			s_edgePen = new Pen(Brushes.Gray, 0.05);
			s_edgePen.Freeze();
		}

		RenderData m_renderData;

		public Renderer(RenderData renderData)
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

		bool m_renderDirection = true;
		bool m_renderTexts = false;

		void RenderTile(DrawingContext dc, int x, int y)
		{
			var data = m_renderData.Grid[y, x];

			var renderSize = new Size(1, 1);

			dc.DrawRectangle(data.Brush, s_edgePen, new Rect(renderSize));

			if (data.Stairs == Stairs.Down)
			{
				double tri = renderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri, tri), new Point(tri * 2, renderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri * 2, renderSize.Height / 2), new Point(tri, tri * 2));
			}
			else if (data.Stairs == Stairs.Up)
			{
				double tri = renderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri * 2, tri), new Point(tri, renderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri, renderSize.Height / 2), new Point(tri * 2, tri * 2));
			}

			if (m_renderDirection && data.From != Direction.None)
			{
				var iv = data.From.ToIntVector2();
				var v = new Vector(iv.X, iv.Y);
				v *= renderSize.Width / 4;
				Point mp = new Point(renderSize.Width / 2, renderSize.Height / 2);
				dc.DrawEllipse(Brushes.White, null, mp, 0.1, 0.1);
				dc.DrawLine(new Pen(Brushes.White, 0.05), mp, mp + new Vector(v.X, v.Y));
			}

			if (data.Weight != 0)
			{
				var ft = new FormattedText(data.Weight.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(renderSize.Width - ft.Width - 0.02, 0));
			}

			if (m_renderTexts && (data.G != 0 || data.H != 0))
			{
				var ft = new FormattedText(data.G.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(0.02, renderSize.Height - ft.Height - 0.02));

				ft = new FormattedText(data.H.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(renderSize.Width - ft.Width - 0.02, renderSize.Height - ft.Height - 0.02));

				ft = new FormattedText((data.G + data.H).ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(0.02, 0.02));
			}
		}

		public void Dispose()
		{
		}
	}
}
