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
		Stream m_drawingStream;
		List<SymbolInfo> m_symbolList;
		List<TerrainInfo> m_terrains;
		List<ObjectInfo> m_objects;

		public AreaData()
		{
			ParseSymbols();
			ParseTerrains();
			ParseObjects();

			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			m_drawingStream = ass.GetManifestResourceStream("MyAreaData.PlanetCute.xaml");
		}

		public IList<SymbolInfo> Symbols { get { return m_symbolList.AsReadOnly(); } }
		public IList<TerrainInfo> Terrains { get { return m_terrains.AsReadOnly(); } }
		public IList<ObjectInfo> Objects { get { return m_objects.AsReadOnly(); } }
		public Stream DrawingStream { get { return m_drawingStream; } }

		void ParseSymbols()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Symbols.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement rootElem = root.Element("Symbols");

			m_symbolList = new List<SymbolInfo>(rootElem.Elements().Count());
			int id = 0;
			foreach (XElement elem in rootElem.Elements())
			{
				var symbol = new SymbolInfo();
				symbol.ID = id++;
				symbol.Name = (string)elem.Element("Name");
				symbol.CharSymbol = ((string)elem.Element("CharSymbol"))[0];
				if (elem.Element("Drawing") != null)
				{
					var drawingElem = elem.Element("Drawing");
					XAttribute attr;

					attr = drawingElem.Attribute("x");
					if (attr != null)
						symbol.X = (double)attr;

					attr = drawingElem.Attribute("y");
					if (attr != null)
						symbol.Y = (double)attr;

					attr = drawingElem.Attribute("w");
					if (attr != null)
						symbol.Width = (double)attr;

					attr = drawingElem.Attribute("h");
					if (attr != null)
						symbol.Height = (double)attr;

					symbol.DrawingName = (string)drawingElem;
				}
				m_symbolList.Add(symbol);
			}
		}

		void ParseTerrains()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Terrains.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement terrainInfosElem = root.Element("TerrainInfos");

			m_terrains = new List<TerrainInfo>(terrainInfosElem.Elements().Count() + 1);
			m_terrains.Add(new TerrainInfo() { ID = 0, SymbolID = 0 });
			int terrainID = 1;
			foreach (XElement terrainElem in terrainInfosElem.Elements())
			{
				TerrainInfo terrain = new TerrainInfo();
				terrain.ID = terrainID++;
				terrain.Name = (string)terrainElem.Element("Name");
				terrain.IsWalkable = (bool)terrainElem.Element("IsWalkable");
				string symbolName = (string)terrainElem.Element("Symbol");
				SymbolInfo symbol = m_symbolList.Single(s => s.Name == symbolName);
				terrain.SymbolID = symbol.ID;
				m_terrains.Add(terrain);
			}
		}

		void ParseObjects()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Objects.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement objectInfosElem = root.Element("Objects");

			m_objects = new List<ObjectInfo>(objectInfosElem.Elements().Count());
			int idx = 0;
			foreach (XElement objectElem in objectInfosElem.Elements())
			{
				ObjectInfo ob = new ObjectInfo();
				ob.SymbolID = idx++;
				ob.Name = (string)objectElem.Element("Name");
				string symbolName = (string)objectElem.Element("Symbol");
				SymbolInfo symbol = m_symbolList.Single(s => s.Name == symbolName);
				ob.SymbolID = symbol.ID;
				m_objects.Add(ob);
			}
		}
	}
}
