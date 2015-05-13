using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.Server.Fortress
{
	public class ArtificialWorldCreator
	{
		const int MAP_SIZE = 5;	// 2^AREA_SIZE

		World m_world;
		EnvironmentObject m_env;
		TerrainData m_terrainData;
		GameMap m_mapMode;

		public EnvironmentObject MainEnv { get { return m_env; } }

		public ArtificialWorldCreator(World world, GameMap mapMode)
		{
			m_mapMode = mapMode;
			m_world = world;
		}

		public void InitializeWorld()
		{
			CreateTerrain();

			var p = new IntVector2(m_terrainData.Width / 2, m_terrainData.Height / 2);
			var start = m_terrainData.GetSurfaceLocation(p);

			m_env = EnvironmentObject.Create(m_world, m_terrainData, VisibilityMode.AllVisible, start);
		}

		void CreateTerrain()
		{
			int side = MyMath.Pow2(MAP_SIZE);
			var size = new IntSize3(side, side, side);

			TerrainData terrain;

			switch (m_mapMode)
			{
				case GameMap.Ball:
					terrain = ArtificialGen.CreateBallMap(size);
					break;
				case GameMap.Cube:
					terrain = ArtificialGen.CreateCubeMap(size, 2);
					break;
				default:
					throw new NotImplementedException();
			}

			m_terrainData = terrain;
		}
	}
}
