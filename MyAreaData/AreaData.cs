using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;
using System.Xml.Linq;
using System.IO;

namespace MyAreaData
{
	public class AreaData : IAreaData
	{
		Terrains m_terrains;
		Stream m_drawingStream;
		Objects m_objects;

		public AreaData()
		{
			ParseTerrains();
			ParseObjects();

			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			m_drawingStream = ass.GetManifestResourceStream("MyAreaData.PlanetCute.xaml");
		}

		public Terrains Terrains
		{
			get { return m_terrains; }
		}

		public Objects Objects
		{
			get { return m_objects; }
		}

		public Stream DrawingStream
		{
			get { return m_drawingStream; }
		}

		void ParseTerrains()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Terrains.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement terrainInfosElem = root.Element("TerrainInfos");

			TerrainInfo[] terrainInfos = new TerrainInfo[terrainInfosElem.Elements().Count() + 1];
			terrainInfos[0] = new TerrainInfo() { TerrainID = 0, CharSymbol = '?' };
			int terrainID = 1;
			foreach (XElement terrainElem in terrainInfosElem.Elements())
			{
				TerrainInfo terrain = new TerrainInfo();
				terrain.TerrainID = terrainID++;
				terrain.Name = (string)terrainElem.Element("Name");
				terrain.IsWalkable = (bool)terrainElem.Element("IsWalkable");
				terrain.CharSymbol = ((string)terrainElem.Element("CharSymbol"))[0];
				terrain.DrawingName = (string)terrainElem.Element("DrawingName");
				terrainInfos[terrain.TerrainID] = terrain;
			}

			m_terrains = new Terrains(terrainInfos);
		}

		void ParseObjects()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Objects.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement objectInfosElem = root.Element("Objects");

			ObjectInfo[] objectInfos = new ObjectInfo[objectInfosElem.Elements().Count()];
			int idx = 0;
			foreach (XElement objectElem in objectInfosElem.Elements())
			{
				ObjectInfo ob = new ObjectInfo();
				ob.SymbolID = idx;
				ob.Name = (string)objectElem.Element("Name");
				ob.CharSymbol = ((string)objectElem.Element("CharSymbol"))[0];
				ob.DrawingName = (string)objectElem.Element("DrawingName");
				objectInfos[idx++] = ob;
			}

			m_objects = new Objects(objectInfos);
		}
	}
}
