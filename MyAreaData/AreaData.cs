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
		public Terrains GetTerrains()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.Symbols.xml");

			XDocument root = XDocument.Load(new StreamReader(resStream));
			XElement terrainInfosElem = root.Element("TerrainInfos");

			TerrainInfo[] terrainInfos = new TerrainInfo[terrainInfosElem.Elements().Count()];
			int terrainID = 0;
			foreach (XElement terrainElem in terrainInfosElem.Elements())
			{
				TerrainInfo terrain = new TerrainInfo();
				terrain.TerrainID = terrainID++;
				terrain.Name = (string)terrainElem.Element("Name");
				terrain.IsWalkable = (bool)terrainElem.Element("IsWalkable");
				string symStr = (string)terrainElem.Element("CharSymbol");
				terrain.CharSymbol = (symStr != null && symStr.Length > 0) ? symStr[0] : '?';
				terrainInfos[terrain.TerrainID] = terrain;
			}

			return new Terrains(terrainInfos);
		}

		public Stream GetPlanetCute()
		{
			var ass = System.Reflection.Assembly.GetExecutingAssembly();
			Stream resStream = ass.GetManifestResourceStream("MyAreaData.PlanetCute.xaml");
			return resStream;
		}
	}
}
