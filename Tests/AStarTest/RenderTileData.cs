using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Dwarrowdelf;
using System.Windows;

namespace AStarTest
{
	class RenderTileData
	{
		static Pen s_edgePen;

		static RenderTileData()
		{
			s_edgePen = new Pen(Brushes.Gray, 0.05);
			s_edgePen.Freeze();
		}

		public RenderTileData()
		{
		}

		public Brush Brush;
		public int G;
		public int H;
		public Direction From;
		public int Weight;
		public Stairs Stairs;

		public void ClearTile()
		{
			this.Brush = Brushes.Black;
			this.G = 0;
			this.H = 0;
			this.From = Direction.None;
			this.Weight = 0;
			this.Stairs = Stairs.None;
		}

		public void OnRender(DrawingContext dc, bool renderDetails)
		{
			var renderSize = new Size(1, 1);

			dc.DrawRectangle(this.Brush, s_edgePen, new Rect(renderSize));

			if (this.Stairs == Stairs.Down)
			{
				double tri = renderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri, tri), new Point(tri * 2, renderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri * 2, renderSize.Height / 2), new Point(tri, tri * 2));
			}
			else if (this.Stairs == Stairs.Up)
			{
				double tri = renderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri * 2, tri), new Point(tri, renderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 0.05), new Point(tri, renderSize.Height / 2), new Point(tri * 2, tri * 2));
			}

			if (!renderDetails)
				return;

			if (From != Direction.None)
			{
				var iv = IntVector2.FromDirection(From);
				var v = new Vector(iv.X, iv.Y);
				v *= renderSize.Width / 4;
				Point mp = new Point(renderSize.Width / 2, renderSize.Height / 2);
				dc.DrawEllipse(Brushes.White, null, mp, 0.1, 0.1);
				dc.DrawLine(new Pen(Brushes.White, 0.05), mp, mp + new Vector(v.X, v.Y));
			}

			if (this.Weight != 0)
			{
				var ft = new FormattedText(this.Weight.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(renderSize.Width - ft.Width - 0.02, 0));
			}

			if (G != 0 || H != 0)
			{
				var ft = new FormattedText(G.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(0.02, renderSize.Height - ft.Height - 0.02));

				ft = new FormattedText(H.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(renderSize.Width - ft.Width - 0.02, renderSize.Height - ft.Height - 0.02));

				ft = new FormattedText((G + H).ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 0.2, Brushes.White);
				dc.DrawText(ft, new Point(0.02, 0.02));
			}
		}
	}
}
