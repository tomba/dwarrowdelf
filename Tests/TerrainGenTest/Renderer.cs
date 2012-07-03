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
		WriteableBitmap m_surfaceBmp;
		WriteableBitmap m_sliceBmp;

		public BitmapSource SurfaceBmp { get { return m_surfaceBmp; } }
		public BitmapSource SliceBmp { get { return m_sliceBmp; } }

		IntSize3 m_size;

		public int Level { get; set; }

		public Renderer(IntSize3 size)
		{
			m_size = size;

			m_surfaceBmp = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgr32, null);
			m_sliceBmp = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgr32, null);
		}

		public void RenderTerrain(ArrayGrid2D<int> m_heightMap)
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

		public void RenderSlice(TileData[, ,] m_grid)
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
						var td = m_grid[p.Z, p.Y, p.X];

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
				var mat = Materials.GetMaterial(td.TerrainMaterialID);
				var rgb = mat.Color.ToGameColorRGB();

				r = rgb.R;
				g = rgb.G;
				b = rgb.B;
			}

			return (uint)((r << 16) | (g << 8) | (b << 0));
		}
	}
}
