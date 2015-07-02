using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Dwarrowdelf.Client
{
	static class TileSetLoaderHelpers
	{
		public static Drawing DrawCharacter(char ch, Typeface typeFace, double fontSize, Color? color, Color? bgColor,
			bool drawOutline, double outlineThickness, bool reverse, CharRenderMode mode)
		{
			DrawingGroup dGroup = new DrawingGroup();
			var brush = color.HasValue ? new SolidColorBrush(color.Value) : new SolidColorBrush(Colors.White);
			var bgBrush = bgColor.HasValue ? new SolidColorBrush(bgColor.Value) : Brushes.Transparent;
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
				var bounds = pen != null ? geometry.GetRenderBounds(pen) : geometry.Bounds;

				Rect bb;

				switch (mode)
				{
					case CharRenderMode.Full:
						{
							double size = formattedText.Height;
							bb = new Rect(bounds.X + bounds.Width / 2 - size / 2, 0, size, size);
						}
						break;

					case CharRenderMode.Caps:
						{
							double size = typeFace.CapsHeight * fontSize;
							bb = new Rect(bounds.X + bounds.Width / 2 - size / 2, formattedText.Baseline - size,
								size, size);
						}
						break;

					case CharRenderMode.Free:
						bb = bounds;
						break;

					default:
						throw new Exception();
				}

				if (reverse)
					geometry = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(bb), geometry);

				//dc.DrawRectangle(bgBrush, new Pen(Brushes.Red, 1), bb);
				dc.DrawRectangle(bgBrush, null, bb);

				dc.DrawGeometry(brush, pen, geometry);

				/*
				var dl = new Action<double>((y) =>
					dc.DrawLine(new Pen(Brushes.Red, 1), new Point(bb.Left, y), new Point(bb.Right, y)));

				dl(0);
				dl(formattedText.Baseline);
				dl(fontSize);
				dl(formattedText.Height);
				dl(formattedText.Baseline - typeFace.CapsHeight * fontSize);
				*/
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
	}
}
