using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MyGame
{
	class MapTile: UIElement
	{
		BitmapSource m_bmp;
		BitmapSource m_objectBmp;

		public MapTile()
		{
			this.IsHitTestVisible = false;
		}

		public BitmapSource Bitmap
		{
			get { return m_bmp; }

			set
			{
				if (m_bmp != value)
				{
					m_bmp = value;
					this.InvalidateVisual();
				}
			}
		}

		public BitmapSource ObjectBitmap
		{
			get { return m_objectBmp; }

			set
			{
				if (m_objectBmp != value)
				{
					m_objectBmp = value;
					this.InvalidateVisual();
				}
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (m_bmp != null)
				drawingContext.DrawImage(m_bmp, new Rect(this.RenderSize));
			else
				drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(0, 0), 
					new Point(this.RenderSize.Width, this.RenderSize.Height));

			if (m_objectBmp != null)
			{
				drawingContext.DrawImage(m_objectBmp, new Rect(this.RenderSize));
			}


#if DEBUGa
			int x = (int)(this.VisualOffset.X / this.RenderSize.Width);
			int y = (int)(this.VisualOffset.Y / this.RenderSize.Width);
			FormattedText ft = new FormattedText(String.Format("{0},{1}", x, y),
				System.Globalization.CultureInfo.GetCultureInfo("en-us"),
				FlowDirection.LeftToRight,
				new Typeface("Verdana"),
				10, Brushes.Black);

			drawingContext.DrawText(ft, new Point(0, 0));
#endif
		}
	}
}
