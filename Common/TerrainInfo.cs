using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum VisibilityMode
	{
		AllVisible,	// everything visible
		SimpleFOV,	// everything inside VisionRange is visible
		LOS,		// use LOS algorithm
	}

	public class TerrainInfo
	{
		public int TerrainID { get; set; }
		public string Name { get; set; }
		public bool IsWalkable { get; set; }
		public char CharSymbol { get; set; }
	}

	public class Terrains
	{
		TerrainInfo[] m_terrains;

		public Terrains(TerrainInfo[] terrains)
		{
			m_terrains = terrains;
		}

		public TerrainInfo FindTerrainByName(string name)
		{
			return m_terrains.First(t => t.Name == name);
		}


		public TerrainInfo this[int id]
		{
			get { return m_terrains[id]; }
		}
	}
}
