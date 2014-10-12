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
using Dwarrowdelf.TerrainGen;

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

		public bool ShowWaterEnabled { get; set; }

		public Renderer(IntSize3 size)
		{
			m_size = size;

			m_sliceBmpXY = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmpXZ = new WriteableBitmap(size.Width, size.Depth, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmpYZ = new WriteableBitmap(size.Depth, size.Height, 96, 96, PixelFormats.Bgr32, null);
		}

		public void Render(TerrainData terrain, IntVector3 pos)
		{
			if (pos.Z == m_size.Depth)
				RenderTerrain(terrain);
			else
				RenderSliceXY(terrain, pos.Z);
			RenderSliceXZ(terrain, pos.Y);
			RenderSliceYZ(terrain, pos.X);
		}

		void RenderTerrain(TerrainData terrain)
		{
			int w = m_size.Width;
			int h = m_size.Height;

			TileData[, ,] tileGrid;
			byte[,] levelMap;
			terrain.GetData(out tileGrid, out levelMap);

			int min = levelMap.Min();
			int max = levelMap.Max();

			m_sliceBmpXY.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpXY.BackBuffer;
				int stride = m_sliceBmpXY.BackBufferStride / 4;

				Parallel.For(0, h, y =>
				{
					for (int x = 0; x < w; ++x)
					{
						int z = terrain.GetSurfaceLevel(x, y);

						TileData td;

						while (true)
						{
							var p = new IntVector3(x, y, z);
							td = terrain.GetTileData(p);

							if (this.ShowWaterEnabled && td.WaterLevel > 0)
							{
								var wl = terrain.GetWaterLevel(p + Direction.Up);
								if (wl > 0)
								{
									z++;
									continue;
								}
							}

							break;
						}

						int m = MyMath.Round(MyMath.LinearInterpolation(min, max, 100, 255, z));

						var cv = GetTileColor(td);

						int r = cv.X;
						int g = cv.Y;
						int b = cv.Z;

						r = r * m / 255;
						g = g * m / 255;
						b = b * m / 255;

						var ptr = pBackBuffer + y * stride + x;

						*ptr = (uint)((r << 16) | (g << 8) | (b << 0));
					}
				});
			}

			m_sliceBmpXY.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXY.PixelWidth, m_sliceBmpXY.PixelHeight));
			m_sliceBmpXY.Unlock();
		}

		uint ColorToRaw(Color c)
		{
			return (uint)((c.R << 16) | (c.G << 8) | (c.B << 0));
		}

		uint IntVector3ToRaw(IntVector3 c)
		{
			return (uint)((c.X << 16) | (c.Y << 8) | (c.Z << 0));
		}

		void RenderSliceXY(TerrainData terrain, int level)
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
						var p = new IntVector3(x, y, level);
						var td = terrain.GetTileData(p);

						uint c;
						if (td.IsEmpty && td.WaterLevel == 0)
							c = ColorToRaw(Colors.SkyBlue);
						else
							c = IntVector3ToRaw(GetTileColor(td));

						var ptr = pBackBuffer + y * stride + x;

						*ptr = c;
					}
				});
			}

			m_sliceBmpXY.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXY.PixelWidth, m_sliceBmpXY.PixelHeight));
			m_sliceBmpXY.Unlock();
		}

		void RenderSliceXZ(TerrainData terrain, int y)
		{
			int w = m_size.Width;
			int h = m_size.Height;
			int d = m_size.Depth;

			m_sliceBmpXZ.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpXZ.BackBuffer;
				int stride = m_sliceBmpXZ.BackBufferStride / 4;

				Parallel.For(0, d, z =>
				{
					for (int x = 0; x < w; ++x)
					{
						int mz = d - z - 1;

						var p = new IntVector3(x, y, mz);
						var td = terrain.GetTileData(p);

						uint c;

						if (td.IsEmpty && td.WaterLevel == 0)
							c = ColorToRaw(Colors.SkyBlue);
						else
							c = IntVector3ToRaw(GetTileColor(td));

						var ptr = pBackBuffer + z * stride + x;

						*ptr = c;
					}
				});
			}

			m_sliceBmpXZ.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpXZ.PixelWidth, m_sliceBmpXZ.PixelHeight));
			m_sliceBmpXZ.Unlock();
		}

		void RenderSliceYZ(TerrainData terrain, int x)
		{
			int w = m_size.Width;
			int h = m_size.Height;
			int d = m_size.Depth;

			m_sliceBmpYZ.Lock();

			unsafe
			{
				var pBackBuffer = (uint*)m_sliceBmpYZ.BackBuffer;
				int stride = m_sliceBmpYZ.BackBufferStride / 4;

				Parallel.For(0, d, z =>
				{
					for (int y = 0; y < h; ++y)
					{
						int mz = d - z - 1;

						var p = new IntVector3(x, y, mz);
						var td = terrain.GetTileData(p);

						uint c;

						if (td.IsEmpty && td.WaterLevel == 0)
							c = ColorToRaw(Colors.SkyBlue);
						else
							c = IntVector3ToRaw(GetTileColor(td));

						var ptr = pBackBuffer + y * stride + z;

						*ptr = c;
					}
				});
			}

			m_sliceBmpYZ.AddDirtyRect(new Int32Rect(0, 0, m_sliceBmpYZ.PixelWidth, m_sliceBmpYZ.PixelHeight));
			m_sliceBmpYZ.Unlock();
		}

		IntVector3 GetTileColor(TileData td)
		{
			byte r, g, b;

			if (td.WaterLevel > 0)
			{
				r = g = 0;
				b = 255;
				return new IntVector3(r, g, b);
			}

			switch (td.TerrainID)
			{
				case TerrainID.Undefined:
					r = g = b = 0;
					break;

				case TerrainID.Empty:
				case TerrainID.Slope:
					r = 0;
					g = 0;
					b = 0;
					break;

				case TerrainID.NaturalFloor:
					switch (td.InteriorID)
					{
						case InteriorID.Empty:
							{
								var mat = td.TerrainMaterialID;

								var matInfo = Materials.GetMaterial(mat);
								var rgb = matInfo.Color.ToGameColorRGB();

								r = rgb.R;
								g = rgb.G;
								b = rgb.B;
							}
							break;

						case InteriorID.NaturalWall:
							{
								var mat = td.InteriorMaterialID;

								var matInfo = Materials.GetMaterial(mat);
								var rgb = matInfo.Color.ToGameColorRGB();

								r = (byte)(rgb.R / 2);
								g = (byte)(rgb.G / 2);
								b = (byte)(rgb.B / 2);
							}
							break;

						case InteriorID.Stairs:
							r = 255;
							g = b = 0;
							break;

						case InteriorID.Grass:
							r = 50;
							b = 0;
							g = 255;
							break;

						case InteriorID.Tree:
							r = b = 0;
							g = 200;
							break;

						default:
							throw new Exception();
					}
					break;

				case TerrainID.StairsDown:
					r = 255;
					g = b = 0;
					break;

				default:
					throw new Exception();
			}

			return new IntVector3(r, g, b);
		}
	}
}
