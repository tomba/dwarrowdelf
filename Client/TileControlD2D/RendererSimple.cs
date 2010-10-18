using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using System.Diagnostics;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public class RendererSimple : IRenderer
	{
		IRenderResolver m_resolver;
		RenderData<RenderTileSimple> m_renderData;

		uint[] m_simpleBitmapArray;

		public RendererSimple(IRenderResolver resolver, RenderData<RenderTileSimple> renderMap)
		{
			m_resolver = resolver;
			m_renderData = renderMap;
		}

		public void RenderTargetChanged()
		{
		}

		public void TileSizeChanged(int tileSize)
		{
		}

		public void SizeChanged()
		{
			m_simpleBitmapArray = null;
		}

		public void Render(RenderTarget renderTarget, int columns, int rows, int tileSize)
		{
			m_renderData.Size = new IntSize(columns, rows);

			m_resolver.Resolve();

			RenderTiles(renderTarget, columns, rows, tileSize);
		}

		unsafe void RenderTiles(RenderTarget renderTarget, int columns, int rows, int tileSize)
		{
			Debug.Print("RendererSimple.RenderTiles");

			uint bytespp = 4;
			uint w = (uint)columns;
			uint h = (uint)rows;

			//var sw = Stopwatch.StartNew();

			if (m_simpleBitmapArray == null)
			{
				Debug.Print("Create SimpleBitmapArray");
				m_simpleBitmapArray = new uint[w * h];
			}

			fixed (uint* a = m_simpleBitmapArray)
			{
				for (int y = 0; y < h; ++y)
				{
					for (int x = 0; x < w; ++x)
					{
						var data = m_renderData.ArrayGrid.Grid[y, x];

						// fast path for black
						if (data.Color.IsEmpty || data.Color == GameColor.Black.ToGameColorRGB())
						{
							a[(h - y - 1) * w + x] = 0;
							continue;
						}

						var rgb = data.Color;
						var m = 1.0 - data.DarknessLevel / 255.0;
						var r = (byte)(rgb.R * m);
						var g = (byte)(rgb.G * m);
						var b = (byte)(rgb.B * m);
						uint c = (uint)((r << 16) | (g << 8) | (b << 0));

						a[(h - y - 1) * w + x] = c;
					}
				}

				var bmp = renderTarget.CreateBitmap(new SizeU(w, h), (IntPtr)a, w * bytespp,
					new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Ignore), 96, 96));
				var destRect = new RectF(0, 0, w * tileSize, h * tileSize);
				renderTarget.DrawBitmap(bmp, 1.0f, BitmapInterpolationMode.NearestNeighbor, destRect);
				bmp.Dispose();
			}

			//sw.Stop();
			//Trace.WriteLine(String.Format("RenderSimpleTiles {0} ms", sw.ElapsedMilliseconds));
		}
	}
}
