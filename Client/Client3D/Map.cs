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

			var terrainData = CreateTerrain(size);

			var voxelMap = new VoxelMap(size);

			foreach (var p in size.Range())
			{
				var td = terrainData.GetTileData(p);

				if (td.WaterLevel > 0)
					voxelMap.SetVoxelDirect(p, Voxel.Water);
				else if (td.IsEmpty)
					voxelMap.SetVoxelDirect(p, Voxel.Empty);
				else
					voxelMap.SetVoxelDirect(p, Voxel.Rock);
			}

			voxelMap.CheckVisibleFaces(false);

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


	}
}
