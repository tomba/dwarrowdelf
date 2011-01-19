using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client.TileControl
{
	class Drawings
	{
		public static BitmapSource[] GetBitmaps(ISymbolDrawingCache symbolDrawingCache, int size)
		{
			var symbolIDArr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			var numDistinctBitmaps = (int)symbolIDArr.Max() + 1;

			var arr = new BitmapSource[numDistinctBitmaps];
			for (int i = 0; i < numDistinctBitmaps; ++i)
				arr[i] = CreateSymbolBitmap(symbolDrawingCache, size, (SymbolID)i);

			return arr;
		}

		static BitmapSource CreateSymbolBitmap(ISymbolDrawingCache symbolDrawingCache, int size, SymbolID symbolID)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			var d = symbolDrawingCache.GetDrawing(symbolID, GameColor.None);
		/*
			d = NormalizeDrawing(d, new Point(0, 0), new Size(100, 100), 0, true);

			drawingContext.PushTransform(new ScaleTransform((double)size / 100, (double)size / 100));
			drawingContext.DrawDrawing(d);
			drawingContext.Pop();
			drawingContext.Close();
			*/
			RenderTargetBitmap bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();

			return bmp;
		}

		static Drawing NormalizeDrawing(Drawing drawing, Point location, Size size, double angle, bool bgTransparent)
		{
			var transform = CreateNormalizeTransform(drawing.Bounds, location, size, angle);

			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.DrawRectangle(bgTransparent ? Brushes.Transparent : Brushes.Black, null, new Rect(new Size(100, 100)));

				dc.PushTransform(transform);

				dc.DrawDrawing(drawing);

				dc.Pop();
			}

			return dGroup;
		}

		static Transform CreateNormalizeTransform(Rect bounds, Point location, Size size, double angle)
		{
			var t = new TransformGroup();

			var b = bounds;

			// Move center of the geometry to origin
			t.Children.Add(new TranslateTransform(-b.X - b.Width / 2, -b.Y - b.Height / 2));
			// Rotate around origin
			t.Children.Add(new RotateTransform(angle));

			b = t.TransformBounds(bounds);
			// Scale to requested size
			t.Children.Add(new ScaleTransform(size.Width / b.Width, size.Height / b.Height));

			b = t.TransformBounds(bounds);
			// Move to requested position
			t.Children.Add(new TranslateTransform(-b.X + location.X, -b.Y + location.Y));

			t.Freeze();

			b = t.TransformBounds(bounds);
			System.Diagnostics.Debug.Assert(Math.Abs(b.X - location.X) < 0.0001);
			System.Diagnostics.Debug.Assert(Math.Abs(b.Y - location.Y) < 0.0001);
			System.Diagnostics.Debug.Assert(Math.Abs(b.Width - size.Width) < 0.0001);
			System.Diagnostics.Debug.Assert(Math.Abs(b.Height - size.Height) < 0.0001);

			return t;
		}
	}
}
