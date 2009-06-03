using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using System.Xml.Linq;
using System.Resources;

namespace MyGame
{
	class WorldDefinition
	{
		public World World { get; private set; }
		public TerrainInfo[] Terrains { get; protected set; }
		MapLevel m_map;

		public WorldDefinition(World world)
		{
			this.World = world;

			System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
			System.IO.Stream resStream = thisExe.GetManifestResourceStream("MyGame.Symbols.xml");

			XDocument root = XDocument.Load(new System.IO.StreamReader(resStream));
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

			this.Terrains = terrainInfos;

			m_map = new MapLevel(this);
		}

		public MapLevel GetLevel(int levelID)
		{
			return m_map;
		}
	}



}
