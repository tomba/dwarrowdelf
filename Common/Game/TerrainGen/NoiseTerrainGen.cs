using SharpNoise;
using SharpNoise.Modules;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dwarrowdelf.TerrainGen
{
	public static class NoiseTerrainGen
	{
		public static TerrainData CreateNoiseTerrain(IntSize3 size, Random random)
		{
			var terrain = new TerrainData(size);

			var noise = CreateTerrainNoise();
			var noisemap = CreateTerrainNoiseMap(noise, new IntSize2(size.Width, size.Height));

			FillFromNoiseMap(terrain, noisemap);

			terrain.RescanLevelMap();

			double xk = (random.NextDouble() * 2 - 1) * 0.01;
			double yk = (random.NextDouble() * 2 - 1) * 0.01;
			TerrainHelpers.CreateBaseMinerals(terrain, random, xk, yk);

			TerrainHelpers.CreateOreVeins(terrain, random, xk, yk);

			TerrainHelpers.CreateOreClusters(terrain, random);

			RiverGen.Generate(terrain, random);

			int soilLimit = size.Depth * 4 / 5;
			TerrainHelpers.CreateSoil(terrain, soilLimit);

			int grassLimit = terrain.Depth * 4 / 5;
			TerrainHelpers.CreateVegetation(terrain, random, grassLimit);

			return terrain;
		}

		static void FillFromNoiseMap(TerrainData terrainData, SharpNoise.NoiseMap noiseMap)
		{
			var max = noiseMap.Data.Max();
			var min = noiseMap.Data.Min();

			Parallel.For(0, noiseMap.Data.Length, i =>
			{
				var v = noiseMap.Data[i];   // [-1 .. 1]

				v -= min;
				v /= (max - min);       // [0 .. 1]

				v *= terrainData.Depth * 8 / 10;
				v += terrainData.Depth * 2 / 10;

				noiseMap.Data[i] = v;
			});

			Parallel.For(0, terrainData.Height, y =>
			{
				for (int x = 0; x < terrainData.Width; ++x)
				{
					var v = noiseMap[x, y];

					int iv = (int)v;

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

		static NoiseMap CreateTerrainNoiseMap(Module noise, IntSize2 size)
		{
			var map = new NoiseMap();

			var build = new SharpNoise.Builders.PlaneNoiseMapBuilder()
			{
				DestNoiseMap = map,
				EnableSeamless = false,
				SourceModule = noise,
			};

			double x = 1;
			double y = 1;
			double w = size.Width / 256.0;
			double h = size.Height / 256.0;

			build.SetDestSize(size.Width, size.Height);
			build.SetBounds(x, x + w, y, y + h);
			build.Build();

			//map.BorderValue = 1;
			//map = NoiseMap.BilinearFilter(map, this.Width, this.Height);

			return map;
		}

		static Module CreateTerrainNoise()
		{
			var mountainTerrain = new RidgedMulti()
			{

			};

			var baseFlatTerrain = new Billow()
			{
				Frequency = 2,
			};

			var flatTerrain = new ScaleBias()
			{
				Source0 = baseFlatTerrain,
				Scale = 0.125,
				Bias = -0.75,
			};

			var terrainType = new Perlin()
			{
				Frequency = 0.5,
				Persistence = 0.25,
			};

			var terrainSelector = new Select()
			{
				Source0 = flatTerrain,
				Source1 = mountainTerrain,
				Control = terrainType,
				LowerBound = 0,
				UpperBound = 1000,
				EdgeFalloff = 0.125,
			};

			var finalTerrain = new Turbulence()
			{
				Source0 = terrainSelector,
				Frequency = 4,
				Power = 0.125,
			};

			return finalTerrain;
		}
	}
}
