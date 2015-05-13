using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.Server.Fortress
{
	public class NoiseWorldCreator
	{
		const int MAP_SIZE = 5;	// 2^AREA_SIZE

		World m_world;
		EnvironmentObject m_env;
		TerrainData m_terrainData;
		GameMap m_mapMode;

		public EnvironmentObject MainEnv { get { return m_env; } }

		public NoiseWorldCreator(World world, GameMap mapMode)
		{
			m_mapMode = mapMode;
			m_world = world;
		}

		public void InitializeWorld()
		{
			CreateTerrain();

			var p = new IntVector2(m_terrainData.Width / 3, m_terrainData.Height / 3);
			var start = m_terrainData.GetSurfaceLocation(p);

			m_env = EnvironmentObject.Create(m_world, m_terrainData, VisibilityMode.AllVisible, start);
		}

		void CreateTerrain()
		{
			var terrain = Dwarrowdelf.TerrainGen.NoiseTerrainGen.CreateNoiseTerrain(new IntSize3(128, 128, 64));
			terrain.RescanLevelMap();
			m_terrainData = terrain;
		}
	}
}
