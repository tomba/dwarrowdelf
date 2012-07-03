using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf;
using System.Windows;
using System.Windows.Media;
using Dwarrowdelf.TerrainGen;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TerrainGenTest
{
	class TerrainWriter
	{
		const int MAP_DEPTH = 20;

		WriteableBitmap m_surfaceBmp;
		WriteableBitmap m_sliceBmp;

		TileData[, ,] m_grid;
		ArrayGrid2D<double> m_doubleHeightMap;
		ArrayGrid2D<int> m_heightMap;

		IntSize3 m_size;

		public BitmapSource SurfaceBmp { get { return m_surfaceBmp; } }
		public BitmapSource SliceBmp { get { return m_sliceBmp; } }

		public TerrainWriter()
		{
			const int depth = 20;
			const int sizeExp = 9;
			int size = (int)Math.Pow(2, sizeExp) + 1;

			m_size = new IntSize3(size, size, depth);

			m_doubleHeightMap = new ArrayGrid2D<double>(size, size);
			m_heightMap = new ArrayGrid2D<int>(size, size);
			m_grid = new TileData[depth, size, size];

			m_surfaceBmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgr32, null);
		}

		public double Average { get; private set; }
		public int Amplify { get; set; }

		public void Generate(DiamondSquare.CornerData corners, double range, double h, int seed)
		{
			m_doubleHeightMap.Clear();

			GenerateTerrain(m_doubleHeightMap, corners, range, h, seed);

			AnalyzeTerrain(m_doubleHeightMap);

			// integer heightmap. the number tells the z level where the floor is.
			foreach (var p in IntPoint2.Range(m_size.Width, m_size.Height))
			{
				var d = m_doubleHeightMap[p];

				d *= MAP_DEPTH / 2;
				d += (MAP_DEPTH / 2) - 1;

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

		public void RenderTerrain()
		{
			int w = m_size.Width;
			int h = m_size.Height;

			m_surfaceBmp.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_surfaceBmp.BackBuffer;
				int stride = m_surfaceBmp.BackBufferStride / 4;

				Parallel.For(0, h, y =>
				{
					for (int x = 0; x < w; ++x)
					{
						var v = m_heightMap[x, y];

						var c = GetColor(v);

						var ptr = pBackBuffer + y * stride + x;

						*ptr = c;
					}
				});
			}

			m_surfaceBmp.AddDirtyRect(new Int32Rect(0, 0, m_surfaceBmp.PixelWidth, m_surfaceBmp.PixelHeight));
			m_surfaceBmp.Unlock();
		}

		uint GetColor(int v)
		{
			uint r, g, b;

			int mountain_min = 10;
			int grass_min = 0;

			if (v >= mountain_min)
			{
				var d = (double)(v - mountain_min) / (MAP_DEPTH - mountain_min);

				Debug.Assert(d >= 0 && d <= 1);

				uint c = 127 + (uint)(d * 127);

				r = c;
				g = c;
				b = c;
			}
			else
			{
				var d = (double)(v - grass_min) / (mountain_min - grass_min);

				Debug.Assert(d >= 0 && d <= 1);

				uint c = 127 / 2 + (uint)(d * 127);

				r = 0;
				g = c;
				b = 0;
			}

			if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
				throw new Exception();

			return (r << 16) | (g << 8) | (b << 0);
		}

		public int Level { get; set; }

		public void RenderSlice()
		{
			int w = m_size.Width;
			int h = m_size.Height;

			m_sliceBmp.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmp.BackBuffer;
				int stride = m_sliceBmp.BackBufferStride / 4;

				Parallel.For(0, h, y =>
				{
					for (int x = 0; x < w; ++x)
					{
						var p = new IntPoint3(x, y, this.Level);
						var td = GetTile(p);

						uint c = GetTileColor(td);

						var ptr = pBackBuffer + y * stride + x;

						*ptr = c;
					}
				});
			}

			m_sliceBmp.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmp.PixelWidth, m_sliceBmp.PixelHeight));
			m_sliceBmp.Unlock();
		}

		uint GetTileColor(TileData td)
		{
			byte r, g, b;

			if (td.IsEmpty)
			{
				r = 0;
				g = 0;
				b = 0;
			}
			else
			{
				r = 128;
				g = 128;
				b = 128;
			}

			return (uint)((r << 16) | (g << 8) | (b << 0));
		}

		void CreateTileGrid()
		{
			int width = m_doubleHeightMap.Width;
			int height = m_doubleHeightMap.Height;
			int depth = m_size.Depth;

			//Parallel.For(0, height, y =>
			for (int y = 0; y < height; ++y)
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
			} //);
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
