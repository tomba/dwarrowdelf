using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;

namespace TerrainGenTest
{
	class Generator
	{
		TileData[, ,] m_grid;
		ArrayGrid2D<double> m_doubleHeightMap;
		ArrayGrid2D<int> m_heightMap;

		IntSize3 m_size;

		public ArrayGrid2D<int> HeightMap { get { return m_heightMap; } }
		public TileData[, ,] TileGrid { get { return m_grid; } }

		public double Average { get; private set; }
		public int Amplify { get; set; }

		public Generator(IntSize3 size)
		{
			m_size = size;

			int w = size.Width;
			int h = size.Height;
			int d = size.Depth;

			m_doubleHeightMap = new ArrayGrid2D<double>(w, h);
			m_heightMap = new ArrayGrid2D<int>(w, h);
			m_grid = new TileData[d, h, w];
		}

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			m_doubleHeightMap.Clear();

			GenerateTerrain(m_doubleHeightMap, corners, range, h, seed);

			AnalyzeTerrain(m_doubleHeightMap);

			// integer heightmap. the number tells the z level where the floor is.
			foreach (var p in IntPoint2.Range(m_size.Width, m_size.Height))
			{
				var d = m_doubleHeightMap[p];

				d *= m_size.Depth / 2;
				d += (m_size.Depth / 2) - 1;

				m_heightMap[p] = (int)Math.Round(d);
			}

			CreateTileGrid();
		}

		void GenerateTerrain(ArrayGrid2D<double> grid, DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			DiamondSquare.Render(grid, corners, range, h, seed);

			//Clamper.Clamp(grid, 10);

			Clamper.Normalize(grid);

			grid.ForEach(v => Math.Pow(v, this.Amplify));

			//Clamper.Normalize(grid);
		}

		void AnalyzeTerrain(ArrayGrid2D<double> grid)
		{
			this.Average = grid.Average();
		}

		void CreateTileGrid()
		{
			int width = m_doubleHeightMap.Width;
			int height = m_doubleHeightMap.Height;
			int depth = m_size.Depth;

			Parallel.For(0, height, y =>
			{
				for (int x = 0; x < width; ++x)
				{
					int surface = m_heightMap[x, y];

					for (int z = 0; z < depth; ++z)
					{
						var p = new IntPoint3(x, y, z);
						var td = new TileData();

						if (z < surface)
						{
							td.TerrainID = TerrainID.NaturalWall;
							td.TerrainMaterialID = MaterialID.Granite;
						}
						else if (z == surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = MaterialID.Granite;
						}
						else
						{
							td.TerrainID = TerrainID.Empty;
							td.TerrainMaterialID = MaterialID.Undefined;
						}

						td.InteriorID = InteriorID.Empty;
						td.InteriorMaterialID = MaterialID.Undefined;

						SetTile(p, td);
					}
				}
			});
		}

		void SetTile(IntPoint3 p, TileData td)
		{
			m_grid[p.Z, p.Y, p.X] = td;
		}

		TileData GetTile(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X];
		}
	}
}
