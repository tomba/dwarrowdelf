using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class Map
	{
		public static VoxelMap Create()
		{
			var size = new IntSize3(128, 128, 64);

			//var terrainData = CreateTerrain(size);
			var terrainData = CreateNoiseTerrain(size);

			var voxelMap = CreateVoxelMap(terrainData);

			return voxelMap;
		}

		static VoxelMap CreateVoxelMap(TerrainData terrainData)
		{
			var voxelMap = new VoxelMap(terrainData.Size);

			foreach (var p in terrainData.Size.Range())
			{
				var td = terrainData.GetTileData(p);

				if (td.WaterLevel > 0)
					voxelMap.SetVoxelDirect(p, Voxel.Water);
				else if (td.IsEmpty)
					voxelMap.SetVoxelDirect(p, Voxel.Empty);
				else if (td.HasTree)
				{
					voxelMap.SetVoxelDirect(p, new Voxel()
					{
						Type = VoxelType.Empty,
						Flags = VoxelFlags.Tree,
					});
				}
				else if (td.IsWall)
					voxelMap.SetVoxelDirect(p, Voxel.Rock);
				else // XXX
					voxelMap.SetVoxelDirect(p, Voxel.Empty);
			}

			voxelMap.CheckVisibleFaces();

			return voxelMap;
		}

		static TerrainData CreateTerrain(IntSize3 size)
		{
			//var random = Helpers.Random;
			var random = new Random(1);

			var terrain = new TerrainData(size);

			var tg = new TerrainGenerator(terrain, random);

			var corners = new DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 2);

			int grassLimit = terrain.Depth * 4 / 5;
			TerrainHelpers.CreateVegetation(terrain, random, grassLimit);

			return terrain;
		}

		static TerrainData CreateNoiseTerrain(IntSize3 size)
		{
			var terrainData = new TerrainData(size);

			var noise = VoxelMapGen.CreateTerrainNoise();
			var noisemap = VoxelMapGen.CreateTerrainNoiseMap(noise, new IntSize2(size.Width, size.Height));

			FillFromNoiseMap(terrainData, noisemap);

			return terrainData;
		}

		static void FillFromNoiseMap(TerrainData terrainData, SharpNoise.NoiseMap noiseMap)
		{
			var max = noiseMap.Data.Max();
			var min = noiseMap.Data.Min();

			Parallel.For(0, noiseMap.Data.Length, i =>
			{
				var v = noiseMap.Data[i];	// [-1 .. 1]

				v -= min;
				v /= (max - min);		// [0 .. 1]

				v *= terrainData.Depth * 8 / 10;
				v += terrainData.Depth * 2 / 10;

				noiseMap.Data[i] = v;
			});

			int waterLimit = terrainData.Depth * 3 / 10;
			int grassLimit = terrainData.Depth * 8 / 10;

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
							if (z < waterLimit)
								terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData); // XXX water
							else
								terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);
						}
						/* surface */
						else if (z == iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);

							if (z >= waterLimit && z < grassLimit)
							{
								Dwarrowdelf.MWCRandom r = new MWCRandom(new IntVector3(x, y, z), 0);
								if (r.Next(100) < 30)
								{
									terrainData.SetTileDataNoHeight(p, new TileData()
									{
										ID = TileID.Tree,
										MaterialID = MaterialID.Fir,
									});
								}
								else
								{
									terrainData.SetTileDataNoHeight(p, new TileData()
									{
										ID = TileID.Grass,
										MaterialID = MaterialID.HairGrass,
									});
								}
							}
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
	}
}
