using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Effects;

namespace MyGame
{
	class SymbolDrawings
	{
		Drawing[] m_symbolDrawings = new Drawing[4];
		ResourceDictionary m_symbolResources;

		public SymbolDrawings()
		{
			m_symbolResources = (ResourceDictionary)Application.LoadComponent(
				new Uri("Symbols/PlanetCute.xaml", UriKind.Relative));

			m_symbolDrawings[0] = CreateUnknownDrawing();
			m_symbolDrawings[1] = GetDrawingByName("Dirt_Block");
			m_symbolDrawings[2] = GetDrawingByName("Stone_Block");
			m_symbolDrawings[3] = GetDrawingByName("Character_Cat_Girl");

			//CreateSymbolDrawings();
		}

		Drawing GetDrawingByName(string name)
		{
			Drawing d = ((DrawingImage)m_symbolResources[name]).Drawing;
			//d = NormalizeDrawing(d);
			return d;
		}

		Drawing NormalizeDrawing(Drawing drawing)
		{
			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				double xc = (drawing.Bounds.Left + drawing.Bounds.Right) / 2;
				double yc = (drawing.Bounds.Top + drawing.Bounds.Bottom) / 2;
				xc = (double)((int)(xc / 32)) * 32;
				yc = (double)((int)(yc / 32)) * 32;
				dc.PushTransform(new TranslateTransform(-xc, -yc));
				dc.DrawDrawing(drawing);
				dc.Pop();
			}
			dGroup.Freeze();
			return dGroup;
		}

		void CreateSymbolDrawings()
		{
			m_symbolDrawings[0] = CreateUnknownDrawing();// CreateDrawing1();
			m_symbolDrawings[1] = CreateDrawing2();
			m_symbolDrawings[2] = CreateDrawing3();
			m_symbolDrawings[3] = CreateDrawing4();
		}


		public Drawing this[int i]
		{
			get
			{
				return m_symbolDrawings[i];
			}
		}

		public int Count { get { return m_symbolDrawings.Length; } }

		Drawing CreateDrawing1()
		{
			Pen shapeOutlinePen = new Pen(Brushes.Black, 2);
			shapeOutlinePen.Freeze();

			DrawingGroup dGroup = new DrawingGroup();

			// Obtain a DrawingContext from 
			// the DrawingGroup.
			using (DrawingContext dc = dGroup.Open())
			{
				// Draw a rectangle at full opacity.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(0, 0, 25, 25));

				// Push an opacity change of 0.5. 
				// The opacity of each subsequent drawing will
				// will be multiplied by 0.5.
				dc.PushOpacity(0.5);

				// This rectangle is drawn at 50% opacity.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(25, 25, 25, 25));

				// Blurs subsquent drawings. 
				dc.PushEffect(new System.Windows.Media.Effects.BlurBitmapEffect(), null);

				// This rectangle is blurred and drawn at 50% opacity (0.5 x 0.5). 
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(50, 50, 25, 25));

				// This rectangle is also blurred and drawn at 50% opacity.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(75, 75, 25, 25));

				// Stop applying the blur to subsquent drawings.
				dc.Pop();

				// This rectangle is drawn at 50% opacity with no blur effect.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(100, 100, 25, 25));
			}

			dGroup.Freeze();
			return dGroup;
		}

		Drawing CreateDrawing3()
		{
			Pen shapeOutlinePen = new Pen(Brushes.Black, 2);
			shapeOutlinePen.Freeze();

			DrawingGroup dGroup = new DrawingGroup();

			// Obtain a DrawingContext from 
			// the DrawingGroup.
			using (DrawingContext dc = dGroup.Open())
			{
				// Draw a rectangle at full opacity.
				dc.DrawRectangle(Brushes.Yellow, shapeOutlinePen, new Rect(0, 0, 25, 25));

				// Push an opacity change of 0.5. 
				// The opacity of each subsequent drawing will
				// will be multiplied by 0.5.
				dc.PushOpacity(0.5);

				// This rectangle is drawn at 50% opacity.
				dc.DrawRectangle(Brushes.Yellow, shapeOutlinePen, new Rect(25, 25, 25, 25));

				// Blurs subsquent drawings. 
				dc.PushEffect(new System.Windows.Media.Effects.BlurBitmapEffect(), null);

				// This rectangle is blurred and drawn at 50% opacity (0.5 x 0.5). 
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(50, 50, 25, 25));

				// This rectangle is also blurred and drawn at 50% opacity.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(75, 75, 25, 25));

				// Stop applying the blur to subsquent drawings.
				dc.Pop();

				// This rectangle is drawn at 50% opacity with no blur effect.
				dc.DrawRectangle(Brushes.Blue, shapeOutlinePen, new Rect(100, 100, 25, 25));
			}

			dGroup.Freeze();
			return dGroup;
		}

		Drawing CreateDrawing2()
		{
			LinearGradientBrush brush = new LinearGradientBrush(
					Colors.Blue,
					Color.FromRgb(204, 204, 255),
					new Point(0, 0),
					new Point(1, 1));
			brush.Freeze();

			Pen p = new Pen(Brushes.Black, 10);
			p.Freeze();

			GeometryGroup ellipses = new GeometryGroup();
			ellipses.Children.Add(
				new EllipseGeometry(new Point(50, 50), 45, 20)
				);
			ellipses.Children.Add(
				new EllipseGeometry(new Point(50, 50), 20, 45)
				);

			var aGeometryDrawing = new GeometryDrawing();
			aGeometryDrawing.Geometry = ellipses;

			aGeometryDrawing.Brush = brush;

			// Outline the drawing with a solid color.
			aGeometryDrawing.Pen = p;

			aGeometryDrawing.Freeze();

			return aGeometryDrawing;
		}

		Drawing CreateDrawing4()
		{
			DrawingGroup mainGroup = new DrawingGroup();

			//
			// Create a GeometryDrawing
			//
			GeometryDrawing ellipseDrawing =
				new GeometryDrawing(
					new SolidColorBrush(Color.FromArgb(102, 181, 243, 20)),
					new Pen(Brushes.Black, 4),
					new EllipseGeometry(new Point(50, 50), 50, 50)
				);

			//
			// Use a DrawingGroup to apply a blur
			// bitmap effect to the drawing.
			//
			DrawingGroup blurGroup = new DrawingGroup();
			blurGroup.Children.Add(ellipseDrawing);
			BlurBitmapEffect blurEffect = new BlurBitmapEffect();
			blurEffect.Radius = 5;
			blurGroup.BitmapEffect = blurEffect;

			// Add the DrawingGroup to the main DrawingGroup.
			mainGroup.Children.Add(blurGroup);


			//
			// Use a DrawingGroup to apply an opacity mask
			// and a bevel.
			//
			DrawingGroup maskedAndBeveledGroup = new DrawingGroup();

			// Create an opacity mask.
			RadialGradientBrush rgBrush = new RadialGradientBrush();
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.55));
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.65));
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.75));
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 0.80));
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.90));
			rgBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 0, 0, 0), 1.0));
			maskedAndBeveledGroup.OpacityMask = rgBrush;

			// Apply a bevel.
			maskedAndBeveledGroup.BitmapEffect = new BevelBitmapEffect();

			// Add the DrawingGroup to the main group.
			mainGroup.Children.Add(maskedAndBeveledGroup);

			//
			// Create another GeometryDrawing.
			//
			GeometryDrawing ellipseDrawing2 =
			  new GeometryDrawing(
				  new SolidColorBrush(Color.FromArgb(102, 181, 243, 20)),
				  new Pen(Brushes.Black, 4),
				  new EllipseGeometry(new Point(150, 150), 50, 50)
			  );

			// Add the DrawingGroup to the main group.
			mainGroup.Children.Add(ellipseDrawing2);

			mainGroup.Freeze();

			return mainGroup;
		}

		Drawing CreateUnknownDrawing()
		{
			DrawingGroup dGroup = new DrawingGroup();

			// Obtain a DrawingContext from 
			// the DrawingGroup.
			using (DrawingContext dc = dGroup.Open())
			{
				FormattedText formattedText = new FormattedText(
						"?",
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						new Typeface("Lucida Console"),
						16,
						Brushes.Black);

				dc.DrawText(formattedText, new Point(0, 0));
			}

			dGroup.Freeze();
			return dGroup;
		}

	}
}