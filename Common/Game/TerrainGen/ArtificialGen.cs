using System;
using System.Threading.Tasks;

namespace Dwarrowdelf.TerrainGen
{
	public static class ArtificialGen
	{
		public static TerrainData CreateBallMap(IntSize3 size, int innerSide = 0)
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

			map.RescanLevelMap();

			return map;
		}

		public static TerrainData CreateCubeMap(IntSize3 size, int margin)
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

			map.RescanLevelMap();

			return map;
		}
	}
}
