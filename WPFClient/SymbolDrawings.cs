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
		Drawing[] m_symbolDrawings = new Drawing[6];
		ResourceDictionary m_symbolResources;

		public SymbolDrawings()
		{
			m_symbolResources = (ResourceDictionary)Application.LoadComponent(
				new Uri("Symbols/PlanetCute.xaml", UriKind.Relative));

			m_symbolDrawings[0] = CreateUnknownDrawing();
			m_symbolDrawings[1] = GetDrawingByName("Dirt_Block");
			m_symbolDrawings[2] = GetDrawingByName("Stone_Block");
			m_symbolDrawings[3] = GetDrawingByName("Character_Cat_Girl");
			m_symbolDrawings[4] = GetDrawingByName("Enemy_Bug");
			m_symbolDrawings[5] = GetDrawingByName("Gem_Blue");
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

		public Drawing this[int i]
		{
			get
			{
				return m_symbolDrawings[i];
			}
		}

		public int Count { get { return m_symbolDrawings.Length; } }

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