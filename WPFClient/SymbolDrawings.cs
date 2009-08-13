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

		class DrawingInfo
		{
			public Point m_location;
			public Size m_size;
		}

		Dictionary<string, DrawingInfo> m_drawingInfos;

		public SymbolDrawings()
		{
			m_drawingInfos = new Dictionary<string, DrawingInfo>();
			DrawingInfo di = new DrawingInfo();
			di.m_location = new Point(10, 10);
			di.m_size = new Size(80, 80);
			m_drawingInfos["Character_Cat_Girl"] = di;

			di = new DrawingInfo();
			di.m_location = new Point(0, 20);
			di.m_size = new Size(100, 60);
			m_drawingInfos["Enemy_Bug"] = di;

			di = new DrawingInfo();
			di.m_location = new Point(0, 0);
			di.m_size = new Size(100, 140);
			m_drawingInfos["Dirt_Block"] = di;

			di = new DrawingInfo();
			di.m_location = new Point(0, 0);
			di.m_size = new Size(100, 140);
			m_drawingInfos["Stone_Block"] = di;

			di = new DrawingInfo();
			di.m_location = new Point(0, 0);
			di.m_size = new Size(100, 100);
			m_drawingInfos["Gem_Blue"] = di;

			IAreaData areaData = new MyAreaData.AreaData();
			var stream = areaData.GetPlanetCute();

			m_symbolResources = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
			/*
			m_symbolResources = (ResourceDictionary)Application.LoadComponent(
				new Uri("Symbols/PlanetCute.xaml", UriKind.Relative));
			*/
			m_symbolDrawings[0] = CreateCharacterDrawing('?');
			m_symbolDrawings[1] = GetDrawingByName("Dirt_Block");
			m_symbolDrawings[2] = GetDrawingByName("Stone_Block");
			m_symbolDrawings[3] = GetDrawingByName("Character_Cat_Girl");
			m_symbolDrawings[4] = GetDrawingByName("Enemy_Bug");
			m_symbolDrawings[5] = GetDrawingByName("Gem_Blue");
		}

		Drawing GetDrawingByName(string name)
		{
			Drawing d = ((DrawingImage)m_symbolResources[name]).Drawing;
			if (m_drawingInfos.ContainsKey(name))
			{
				DrawingInfo di = m_drawingInfos[name];
				d = NormalizeDrawing(d, di.m_location, di.m_size);
			}
			else
			{
				d = NormalizeDrawing(d, new Point(), new Size(100, 100));
			}
			return d;
		}

		Drawing NormalizeDrawing(Drawing drawing, Point location, Size size)
		{
			DrawingGroup dGroup = new DrawingGroup();
			using (DrawingContext dc = dGroup.Open())
			{
				dc.PushTransform(new TranslateTransform(location.X, location.Y));
				dc.PushTransform(new ScaleTransform(size.Width / drawing.Bounds.Width, 
					size.Height / drawing.Bounds.Height));
				dc.PushTransform(new TranslateTransform(-drawing.Bounds.Left, -drawing.Bounds.Top));
				dc.DrawDrawing(drawing);
				dc.Pop();
				dc.Pop();
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

		public Drawing[] Drawings { get { return m_symbolDrawings; } }

		Drawing CreateCharacterDrawing(char c)
		{
			DrawingGroup dGroup = new DrawingGroup();

			using (DrawingContext dc = dGroup.Open())
			{
				FormattedText formattedText = new FormattedText(
						c.ToString(),
						System.Globalization.CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight,
						new Typeface("Lucida Console"),
						16,
						Brushes.White);

				dc.DrawText(formattedText, new Point(0, 0));
			}

			return NormalizeDrawing(dGroup, new Point(), new Size(100, 100));
		}

	}
}