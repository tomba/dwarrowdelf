using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;

namespace Dwarrowdelf.TerrainGen
{
	public class TerrainGenerator
	{
		IntSize3 m_size;
		TerrainData m_data;

		Random m_random;

		public TerrainGenerator(TerrainData data, Random random)
		{
			m_data = data;
			m_size = data.Size;
			m_random = random;
		}

		void FillMap()
		{
			var terrainData = m_data;

			Parallel.For(0, terrainData.Height, y =>
			{
				for (int x = 0; x < terrainData.Width; ++x)
				{
					int iv = m_data.GetSurfaceLevel(x, y);

					for (int z = terrainData.Depth - 1; z >= 0; --z)
					{
						var p = new IntVector3(x, y, z);

						/* above ground */
						if (z > iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);
						}
						/* surface */
						else if (z == iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);
						}
						/* underground */
						else if (z < iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else
						{
							throw new Exception();
						}
					}
				}
			});
		}

		public void Generate(DiamondSquare.CornerData corners, double range, double h, double amplify)
		{
			GenerateHeightMap(corners, range, h, amplify);

			FillMap();

			var random = m_random;
			var terrain = m_data;

			double xk = (random.NextDouble() * 2 - 1) * 0.01;
			double yk = (random.NextDouble() * 2 - 1) * 0.01;
			TerrainHelpers.CreateBaseMinerals(terrain, random, xk, yk);

			TerrainHelpers.CreateOreVeins(terrain, random, xk, yk);

			TerrainHelpers.CreateOreClusters(terrain, random);

			if (m_data.Width > 128)
			{
				var riverGen = new RiverGen(m_data, m_random);
				if (riverGen.CreateRiverPath())
				{
					riverGen.AdjustRiver();
				}
				else
				{
					Trace.TraceError("Failed to create river");
				}
			}

			int soilLimit = m_size.Depth * 4 / 5;
			TerrainHelpers.CreateSoil(m_data, soilLimit);
		}

		void GenerateHeightMap(DiamondSquare.CornerData corners, double range, double h, double amplify)
		{
			// +1 for diamond square
			var heightMap = new ArrayGrid2D<double>(m_size.Width + 1, m_size.Height + 1);

			double min, max;

			DiamondSquare.Render(heightMap, corners, range, h, m_random, out min, out max);

			Parallel.For(0, m_size.Height, y =>
				{
					double d = max - min;

					for (int x = 0; x < m_size.Width; ++x)
					{
						var v = heightMap[x, y];

						// normalize to 0.0 - 1.0
						v = (v - min) / d;

						// amplify
						v = Math.Pow(v, amplify);

						// adjust
						v *= m_size.Depth / 2;
						v += m_size.Depth / 2 - 1;

						m_data.SetSurfaceLevel(x, y, MyMath.Round(v));
					}
				});
		}
	}
}
