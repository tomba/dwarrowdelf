using System;
using System.Threading.Tasks;

namespace Dwarrowdelf.TerrainGen
{
	public static class ArtificialGen
	{
#if NOISE_INCLUDED
		public static TerrainData CreateNoiseTerrain(IntSize3 size)
		{
			var terrainData = new TerrainData(size);

			var noise = VoxelMapGen.CreateTerrainNoise();
			var noisemap = VoxelMapGen.CreateTerrainNoiseMap(noise, new IntSize2(size.Width, size.Height));

			FillFromNoiseMap(terrainData, noisemap);

			return terrainData;
		}

		public static void FillFromNoiseMap(TerrainData terrainData, SharpNoise.NoiseMap noiseMap)
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
#endif
		static TerrainData CreateBallMap(IntSize3 size, int innerSide = 0)
		{
			var map = new TerrainData(size);

			int side = MyMath.Min(size.Width, size.Height, size.Depth);

			int r = side / 2 - 1;
			int ir = innerSide / 2 - 1;

			Parallel.For(0, size.Depth, z =>
			{
				for (int y = 0; y < size.Height; ++y)
					for (int x = 0; x < size.Width; ++x)
					{
						var pr = Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r) + (z - r) * (z - r));

						var p = new IntVector3(x, y, z);

						if (pr < r && pr >= ir)
							map.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
						else
							map.SetTileDataNoHeight(p, TileData.EmptyTileData);
					}
			});

			return map;
		}

		static TerrainData CreateCubeMap(IntSize3 size, int margin)
		{
			var map = new TerrainData(size);

			Parallel.For(0, size.Depth, z =>
			{
				for (int y = 0; y < size.Height; ++y)
					for (int x = 0; x < size.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						if (x < margin || y < margin || z < margin ||
							x >= size.Width - margin || y >= size.Height - margin || z >= size.Depth - margin)
							map.SetTileDataNoHeight(p, TileData.EmptyTileData);
						else
							map.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
					}
			});

			return map;
		}
	}
}
