using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client.Symbols
{
	static class TileSetHelpers
	{
		public static Drawing DrawCharacter(char ch, Typeface typeFace, double fontSize, GameColor color, GameColor bgColor,
			bool drawOutline, double outlineThickness, bool reverse)
		{
			Color c;
			if (color == GameColor.None)
				c = Colors.White;
			else
				c = color.ToWindowsColor();

			DrawingGroup dGroup = new DrawingGroup();
			var brush = new SolidColorBrush(c);
			var bgBrush = bgColor != GameColor.None ? new SolidColorBrush(bgColor.ToWindowsColor()) : Brushes.Transparent;
			using (DrawingContext dc = dGroup.Open())
			{
				var formattedText = new FormattedText(
						ch.ToString(),
						System.Globalization.CultureInfo.InvariantCulture,
						FlowDirection.LeftToRight,
						typeFace,
						fontSize, Brushes.Black);


				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));
				var pen = drawOutline ? new Pen(Brushes.Black, outlineThickness) : null;
				var boundingGeometry = new RectangleGeometry(pen != null ? geometry.GetRenderBounds(pen) : geometry.Bounds);

				if (reverse)
					geometry = new CombinedGeometry(GeometryCombineMode.Exclude, boundingGeometry, geometry);

				dc.DrawGeometry(bgBrush, null, boundingGeometry);
				dc.DrawGeometry(brush, pen, geometry);
			}

			return dGroup;
		}

		public static Drawing NormalizeDrawing(Drawing drawing, Point location, Size size, double angle, bool bgTransparent, double? opacity)
		{
			var transform = CreateNormalizeTransform(drawing.Bounds, location, size, angle);

			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.DrawRectangle(bgTransparent ? Brushes.Transparent : Brushes.Black, null, new Rect(new Size(100, 100)));

				dc.PushTransform(transform);

				if (opacity.HasValue)
					dc.PushOpacity(opacity.Value);

				dc.DrawDrawing(drawing);

				if (opacity.HasValue)
					dc.Pop();

				dc.Pop();
			}

			return dGroup;
		}

		static Transform CreateNormalizeTransform(Rect bounds, Point location, Size size, double angle)
		{
			if (bounds.IsEmpty)
				return Transform.Identity;

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
			Debug.Assert(Math.Abs(b.X - location.X) < 0.0001);
			Debug.Assert(Math.Abs(b.Y - location.Y) < 0.0001);
			Debug.Assert(Math.Abs(b.Width - size.Width) < 0.0001);
			Debug.Assert(Math.Abs(b.Height - size.Height) < 0.0001);

			return t;
		}


		public static void ColorizeDrawing(Drawing drawing, Color tintColor)
		{
			if (drawing is DrawingGroup)
			{
				var dg = (DrawingGroup)drawing;
				foreach (var d in dg.Children)
				{
					ColorizeDrawing(d, tintColor);
				}
			}
			else if (drawing is GeometryDrawing)
			{
				var gd = (GeometryDrawing)drawing;
				if (gd.Brush != null)
					ColorizeBrush(gd.Brush, tintColor);
				if (gd.Pen != null)
					ColorizeBrush(gd.Pen.Brush, tintColor);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		static void ColorizeBrush(Brush brush, Color tintColor)
		{
			if (brush is SolidColorBrush)
			{
				var b = (SolidColorBrush)brush;
				b.Color = TintColor(b.Color, tintColor);
			}
			else if (brush is LinearGradientBrush)
			{
				var b = (LinearGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = TintColor(stop.Color, tintColor);
			}
			else if (brush is RadialGradientBrush)
			{
				var b = (RadialGradientBrush)brush;
				foreach (var stop in b.GradientStops)
					stop.Color = TintColor(stop.Color, tintColor);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public static Color TintColor(Color c, Color tint)
		{
			return SimpleTint(c, tint);
#if asd
			double th, ts, tl;
			HSL.RGB2HSL(tint, out th, out ts, out tl);

			double ch, cs, cl;
			HSL.RGB2HSL(c, out ch, out cs, out cl);

			Color color = HSL.HSL2RGB(th, ts, cl);
			color.A = c.A;

			return color;
#endif
		}

		static Color SimpleTint(Color c, Color tint)
		{
			return Color.FromScRgb(
				c.ScA * tint.ScA,
				c.ScR * tint.ScR,
				c.ScG * tint.ScG,
				c.ScB * tint.ScB);
		}

		public static BitmapSource DrawingToBitmap(Drawing drawing, int size)
		{
			DrawingVisual drawingVisual = new DrawingVisual();
			DrawingContext drawingContext = drawingVisual.RenderOpen();

			drawingContext.PushTransform(new ScaleTransform((double)size / 100, (double)size / 100));
			drawingContext.DrawDrawing(drawing);
			drawingContext.Pop();
			drawingContext.Close();

			RenderTargetBitmap bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Default);
			bmp.Render(drawingVisual);
			bmp.Freeze();

			return bmp;
		}

		public static Drawing BitmapToDrawing(BitmapSource bitmap)
		{
			var dg = new DrawingGroup();

			using (var dc = dg.Open())
			{
				dc.DrawImage(bitmap, new Rect(0, 0, 100, 100));
			}

			return dg;
		}

		public static BitmapSource ColorizeBitmap(BitmapSource bmp, Color tint)
		{
			var wbmp = new WriteableBitmap(bmp);
			var arr = new uint[wbmp.PixelWidth * wbmp.PixelHeight];

			wbmp.CopyPixels(arr, wbmp.PixelWidth * 4, 0);

			for (int i = 0; i < arr.Length; ++i)
			{
				byte a = (byte)((arr[i] >> 24) & 0xff);
				byte r = (byte)((arr[i] >> 16) & 0xff);
				byte g = (byte)((arr[i] >> 8) & 0xff);
				byte b = (byte)((arr[i] >> 0) & 0xff);

				var c = Color.FromArgb(a, r, g, b);
				c = TileSetHelpers.TintColor(c, tint);

				arr[i] = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | (c.B << 0));
			}

			wbmp.WritePixels(new Int32Rect(0, 0, wbmp.PixelWidth, wbmp.PixelHeight), arr, wbmp.PixelWidth * 4, 0);

			return wbmp;
		}
	}
}
