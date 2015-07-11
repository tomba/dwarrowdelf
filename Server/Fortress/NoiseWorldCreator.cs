using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.Server.Fortress
{
	public class NoiseWorldCreator
	{
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

		public void InitializeWorld(IntSize3 size)
		{
			CreateTerrain(size);

			var p = new IntVector2(m_terrainData.Width / 3, m_terrainData.Height / 3);
			var start = m_terrainData.GetSurfaceLocation(p);

			m_env = EnvironmentObject.Create(m_world, m_terrainData, VisibilityMode.AllVisible, start);
		}

		void CreateTerrain(IntSize3 size)
		{
			var random = new Random(1);
			m_terrainData = NoiseTerrainGen.CreateNoiseTerrain(size, random);
		}
	}
}
