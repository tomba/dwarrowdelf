using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;
using SharpNoise;
using SharpNoise.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGenTest
{
	static class Gen
	{
		public static void Generate(TerrainData data)
		{
			var noise = CreateTerrainNoise();

			var noisemap = CreateTerrainNoiseMap(noise, new IntSize2(data.Width, data.Height));

			FillFromNoiseMap(data, noisemap);
		}

		public static void FillFromNoiseMap(TerrainData data, NoiseMap map)
		{
			var max = map.Data.Max();
			var min = map.Data.Min();

			Parallel.For(0, map.Data.Length, i =>
			{
				var v = map.Data[i];	// [-1 .. 1]

				v -= min;
				v /= (max - min);		// [0 .. 1]

				v *= (data.Depth - 1) * 8 / 10;
				v += (data.Depth - 1) * 2 / 10;

				map.Data[i] = v;
			});

			TileData[, ,] grid;
			byte[,] levelMap;

			data.GetData(out grid, out levelMap);

			int waterLimit = data.Depth * 3 / 10;
			int grassLimit = data.Depth * 8 / 10;

			Parallel.For(0, data.Height, y =>
			{
				for (int x = 0; x < data.Width; ++x)
				{
					var v = map[x, y];

					int iv = (int)v;

					levelMap[y, x] = (byte)iv;

					for (int z = data.Depth - 1; z >= 0; --z)
					{
						/* above ground */
						if (z > iv)
						{
							/*
							if (z < waterLimit)
								grid[z, y, x] = Voxel.Water;
							else*/

							grid[z, y, x] = TileData.EmptyTileData;
						}
						/* surface */
						else if (z == iv)
						{
							/*						grid[z, y, x] = Voxel.Rock;

													if (z >= waterLimit && z < grassLimit)
													{
														grid[z, y, x].Flags = VoxelFlags.Grass;

														Dwarrowdelf.MWCRandom r = new MWCRandom(new IntVector3(x, y, z), 0);
														if (r.Next(100) < 30)
														{
															grid[z + 1, y, x].Flags |= VoxelFlags.Tree;
															//grid[z, y, x].Flags |= VoxelFlags.Tree2;
														}
													}
						*/
							grid[z, y, x] = new TileData()
							{
								TerrainID = TerrainID.NaturalFloor,
								TerrainMaterialID = MaterialID.Granite,
								InteriorID = InteriorID.NaturalWall,
								InteriorMaterialID = MaterialID.Granite,
							};
						}
						/* underground */
						else if (z < iv)
						{
							grid[z, y, x] = new TileData()
							{
								TerrainID = TerrainID.NaturalFloor,
								TerrainMaterialID = MaterialID.Granite,
								InteriorID = InteriorID.NaturalWall,
								InteriorMaterialID = MaterialID.Granite,
							};
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

			build.SetDestSize(size.Width, size.Height);
			build.SetBounds(0.5, 1.5, 0.5, 1.5);
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
