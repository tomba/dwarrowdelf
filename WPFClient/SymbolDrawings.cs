using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace MyGame
{
	class SymbolDrawings
	{
		Drawing[] m_objectDrawings;
		Drawing[] m_terrainDrawings;

		ResourceDictionary m_symbolResources;

		class DrawingInfo
		{
			public Point m_location;
			public Size m_size;
		}

		Dictionary<string, DrawingInfo> m_drawingInfos;

		public SymbolDrawings()
		{
			bool useChars = true;

			// this data should be in the AreaData
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
			var stream = areaData.DrawingStream;

			m_symbolResources = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);

			var obs = areaData.Objects;
			m_objectDrawings = new Drawing[obs.Count];
			for (int i = 0; i < obs.Count; i++)
			{
				Drawing drawing;
				if (useChars)
					drawing = CreateCharacterDrawing(obs[i].CharSymbol);
				else
					drawing = GetDrawingByName(obs[i].DrawingName);
				m_objectDrawings[i] = drawing;
			}

			var terrains = areaData.Terrains;
			m_terrainDrawings = new Drawing[terrains.Count];
			for (int i = 0; i < terrains.Count; i++)
			{
				Drawing drawing;
				if (useChars)
				{
					drawing = CreateCharacterDrawing(terrains[i].CharSymbol);
				}
				else
				{
					if (terrains[i].DrawingName == null)
						drawing = CreateCharacterDrawing('?');
					else
						drawing = GetDrawingByName(terrains[i].DrawingName);
				}
				m_terrainDrawings[i] = drawing;
			}
			/*
			m_symbolDrawings[0] = CreateCharacterDrawing('?');
			m_symbolDrawings[1] = GetDrawingByName("Dirt_Block");
			m_symbolDrawings[2] = GetDrawingByName("Stone_Block");
			m_symbolDrawings[3] = GetDrawingByName("Character_Cat_Girl");
			m_symbolDrawings[4] = GetDrawingByName("Enemy_Bug");
			m_symbolDrawings[5] = GetDrawingByName("Gem_Blue");
			 */
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

		public Drawing[] TerrainDrawings { get { return m_terrainDrawings; } }
		public Drawing[] ObjectDrawings { get { return m_objectDrawings; } }

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

				// draw black background, for two reasons. First, to cover the terrain below an object,
				// second, to move the drawn text properly. There's probably a better way to do the second
				// one.
				dc.DrawRectangle(Brushes.Black, null, new Rect(new Size(formattedText.Width, formattedText.Height)));
				dc.DrawText(formattedText, new Point(0, 0));
			}

			return NormalizeDrawing(dGroup, new Point(), new Size(100, 100));
		}

	}
}