using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using Dwarrowdelf;

namespace TerrainGenTest
{
	class Renderer
	{
		WriteableBitmap m_sliceBmpXY;
		WriteableBitmap m_sliceBmpXZ;
		WriteableBitmap m_sliceBmpYZ;

		public BitmapSource SliceBmpXY { get { return m_sliceBmpXY; } }
		public BitmapSource SliceBmpXZ { get { return m_sliceBmpXZ; } }
		public BitmapSource SliceBmpYZ { get { return m_sliceBmpYZ; } }

		IntSize3 m_size;

		public Renderer(IntSize3 size)
		{
			m_size = size;

			m_sliceBmpXY = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmpXZ = new WriteableBitmap(size.Width, size.Depth, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmpYZ = new WriteableBitmap(size.Depth, size.Height, 96, 96, PixelFormats.Bgr32, null);
		}

		public void Render(ArrayGrid2D<int> heightMap, TileData[, ,] grid, IntPoint3 pos)
		{
			if (pos.Z == m_size.Depth)
				RenderTerrain(heightMap);
			else
				RenderSliceXY(grid, pos.Z);
			RenderSliceXZ(grid, pos.Y);
			RenderSliceYZ(grid, pos.X);
		}

		void RenderTerrain(ArrayGrid2D<int> m_heightMap)
		{
			int w = m_size.Width;
			int h = m_size.Height;

			m_sliceBmpXY.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpXY.BackBuffer;
				int stride = m_sliceBmpXY.BackBufferStride / 4;

				Parallel.For(0, h, y =>
				{
					for (int x = 0; x < w; ++x)
					{
						var v = m_heightMap[x, y];

						var c = GetTerrainColor(v);

						var ptr = pBackBuffer + y * stride + x;

						*ptr = c;
					}
				});
			}

			m_sliceBmpXY.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXY.PixelWidth, m_sliceBmpXY.PixelHeight));
			m_sliceBmpXY.Unlock();
		}

		uint GetTerrainColor(int v)
		{
			uint r, g, b;

			int mountain_min = 10;
			int grass_min = 0;

			if (v >= mountain_min)
			{
				var d = (double)(v - mountain_min) / (m_size.Depth - mountain_min);

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

		void RenderSliceXY(TileData[, ,] grid, int level)
		{
			int w = m_size.Width;
			int h = m_size.Height;

			m_sliceBmpXY.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpXY.BackBuffer;
				int stride = m_sliceBmpXY.BackBufferStride / 4;

				Parallel.For(0, h, y =>
				{
					for (int x = 0; x < w; ++x)
					{
						var p = new IntPoint3(x, y, level);
						var td = grid[p.Z, p.Y, p.X];

						uint c = GetTileColor(td);

						var ptr = pBackBuffer + y * stride + x;

						*ptr = c;
					}
				});
			}

			m_sliceBmpXY.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXY.PixelWidth, m_sliceBmpXY.PixelHeight));
			m_sliceBmpXY.Unlock();
		}

		void RenderSliceXZ(TileData[, ,] grid, int y)
		{
			int w = m_size.Width;
			int h = m_size.Height;
			int d = m_size.Depth;

			m_sliceBmpXZ.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpXZ.BackBuffer;
				int stride = m_sliceBmpXZ.BackBufferStride / 4;

				for (int z = 0; z < d; ++z)
				{
					for (int x = 0; x < w; ++x)
					{
						int mz = d - z - 1;

						var p = new IntPoint3(x, y, mz);
						var td = grid[p.Z, p.Y, p.X];

						uint c = GetTileColor(td);

						var ptr = pBackBuffer + z * stride + x;

						*ptr = c;
					}
				}
			}

			m_sliceBmpXZ.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXZ.PixelWidth, m_sliceBmpXZ.PixelHeight));
			m_sliceBmpXZ.Unlock();
		}

		void RenderSliceYZ(TileData[, ,] grid, int x)
		{
			int w = m_size.Width;
			int h = m_size.Height;
			int d = m_size.Depth;

			m_sliceBmpYZ.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpYZ.BackBuffer;
				int stride = m_sliceBmpYZ.BackBufferStride / 4;

				for (int z = 0; z < d; ++z)
				{
					for (int y = 0; y < h; ++y)
					{
						var p = new IntPoint3(x, y, z);
						var td = grid[p.Z, p.Y, p.X];

						uint c = GetTileColor(td);

						var ptr = pBackBuffer + y * stride + z;

						*ptr = c;
					}
				}
			}

			m_sliceBmpYZ.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpYZ.PixelWidth, m_sliceBmpYZ.PixelHeight));
			m_sliceBmpYZ.Unlock();
		}

		uint GetTileColor(TileData td)
		{
			byte r, g, b;

			switch (td.TerrainID)
			{
				case TerrainID.NaturalWall:
					MaterialID mat;

					switch (td.InteriorID)
					{
						case InteriorID.Empty:
							mat = td.TerrainMaterialID;
							break;

						case InteriorID.Ore:
							mat = td.InteriorMaterialID;
							break;

						default:
							throw new Exception();
					}

					var matInfo = Materials.GetMaterial(mat);
					var rgb = matInfo.Color.ToGameColorRGB();

					r = rgb.R;
					g = rgb.G;
					b = rgb.B;
					break;

				case TerrainID.Empty:
				case TerrainID.NaturalFloor:
					r = 0;
					g = 0;
					b = 0;
					break;

				default:
					throw new Exception();
			}

			return (uint)((r << 16) | (g << 8) | (b << 0));
		}
	}
}
