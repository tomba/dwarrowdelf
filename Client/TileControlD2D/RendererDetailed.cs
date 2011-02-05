//#define DEBUG_TEXT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SlimDX.Direct2D;
using SlimDX;
using RectF = System.Drawing.RectangleF;
using DXGI = SlimDX.DXGI;

namespace Dwarrowdelf.Client.TileControl
{
	public class RendererDetailed : IRenderer, IDisposable
	{
		RenderData<RenderTileDetailed> m_renderData;

		Bitmap m_atlasBitmap;

		SolidColorBrush m_bgBrush;
		SolidColorBrush m_darkBrush;

		Dictionary<GameColor, Bitmap>[] m_colorTileArray;

		ISymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_bitmapCache;

		public RendererDetailed(RenderData<RenderTileDetailed> renderMap)
		{
			m_renderData = renderMap;
		}


		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;
			}
		}

		void ClearAtlasBitmap()
		{
			if (m_atlasBitmap != null)
			{
				m_atlasBitmap.Dispose();
				m_atlasBitmap = null;
			}
		}

		void ClearColorTileArray()
		{
			if (m_colorTileArray != null)
			{
				foreach (var entry in m_colorTileArray)
				{
					if (entry == null)
						continue;

					foreach (var kvp in entry)
						kvp.Value.Dispose();
				}

				m_colorTileArray = null;
			}
		}

		void InvalidateBitmaps()
		{
			ClearAtlasBitmap();
			ClearColorTileArray();
		}

		public void InvalidateSymbols()
		{
			InvalidateBitmaps();
			m_bitmapCache.Invalidate();
		}

		public void RenderTargetChanged()
		{
			InvalidateBitmaps();

			if (m_bgBrush != null)
			{
				m_bgBrush.Dispose();
				m_bgBrush = null;
			}

			if (m_darkBrush != null)
			{
				m_darkBrush.Dispose();
				m_darkBrush = null;
			}
		}

		public void TileSizeChanged(int tileSize)
		{
			InvalidateBitmaps();
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = tileSize;
		}

		void CreateAtlas(RenderTarget renderTarget, int tileSize)
		{
			//Debug.WriteLine("CreateAtlas");

			var numTiles = m_bitmapCache.NumDistinctBitmaps;

			const int bytesPerPixel = 4;
			var arr = new byte[tileSize * tileSize * bytesPerPixel];

			m_atlasBitmap = new Bitmap(renderTarget, new System.Drawing.Size(tileSize * numTiles, tileSize),
				new BitmapProperties() { PixelFormat = new PixelFormat(SlimDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), HorizontalDpi = 96, VerticalDpi = 96, });

			for (int x = 0; x < numTiles; ++x)
			{
				var bmp = m_bitmapCache.GetBitmap((SymbolID)x, GameColor.None);
				bmp.CopyPixels(arr, tileSize * 4, 0);
				m_atlasBitmap.FromMemory(arr, tileSize * 4, new System.Drawing.Rectangle(x * tileSize, 0, tileSize, tileSize));
			}
		}



		public void Render(RenderTarget renderTarget, int columns, int rows, int tileSize)
		{
			if (m_symbolDrawingCache == null)
				return;

			if (m_bitmapCache == null)
				m_bitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, tileSize);

			if (m_atlasBitmap == null)
				CreateAtlas(renderTarget, tileSize);

			RenderTiles(renderTarget, columns, rows, tileSize);
		}

		void RenderTiles(RenderTarget renderTarget, int columns, int rows, int tileSize)
		{
#if DEBUG_TEXT
			var blackBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(255, 255, 255, 1));
#endif
			if (m_bgBrush == null)
				m_bgBrush = new SolidColorBrush(renderTarget, new Color4(1.0f, 0, 0, 0));

			if (m_darkBrush == null)
				m_darkBrush = new SolidColorBrush(renderTarget, new Color4(1.0f, 0, 0, 0));

			for (int y = 0; y < rows && y < m_renderData.Size.Height; ++y)
			{
				for (int x = 0; x < columns && x < m_renderData.Size.Width; ++x)
				{
					var x1 = x * tileSize;
					var y1 = y * tileSize;
					var dstRect = new RectF(x1, y1, tileSize, tileSize);

					var data = m_renderData.ArrayGrid.Grid[y, x];

					var d1 = (data.FloorDarknessLevel - data.InteriorDarknessLevel) / 127f;
					var d2 = (data.InteriorDarknessLevel - data.ObjectDarknessLevel) / 127f;
					var d3 = (data.ObjectDarknessLevel - data.TopDarknessLevel) / 127f;
					var d4 = (data.TopDarknessLevel) / 127f;

					var o4 = d4;
					var o3 = d3 / (1f - d4);
					var o2 = d2 / (1f - (d3 + d4));
					var o1 = d1 / (1f - (d2 + d3 + d4));

					DrawTile(renderTarget, tileSize, ref dstRect, ref data.Floor, o1);
					DrawTile(renderTarget, tileSize, ref dstRect, ref data.Interior, o2);
					DrawTile(renderTarget, tileSize, ref dstRect, ref data.Object, o3);
					DrawTile(renderTarget, tileSize, ref dstRect, ref data.Top, o4);
#if DEBUG_TEXT
					m_renderTarget.DrawRectangle(dstRect, blackBrush, 1);
					m_renderTarget.DrawText(String.Format("{0},{1}", x, y), textFormat, dstRect, blackBrush);
#endif
				}
			}
		}

		void DrawTile(RenderTarget renderTarget, int tileSize, ref RectF dstRect, ref RenderTileLayer tile, float darkOpacity)
		{
			if (tile.SymbolID != SymbolID.Undefined && darkOpacity < 0.99f)
			{
				if (tile.BgColor != GameColor.None)
				{
					var rgb = tile.BgColor.ToGameColorRGB();
					m_bgBrush.Color = new Color4(1.0f, (float)rgb.R / 255, (float)rgb.G / 255, (float)rgb.B / 255);
					renderTarget.FillRectangle(m_bgBrush, dstRect);
				}

				if (tile.Color != GameColor.None)
					DrawColoredTile(renderTarget, tileSize, ref dstRect, tile.SymbolID, tile.Color, 1.0f);
				else
					DrawUncoloredTile(renderTarget, tileSize, ref dstRect, tile.SymbolID, 1.0f);
			}

			if (darkOpacity > 0.01f)
			{
				m_darkBrush.Color = new Color4(darkOpacity, 0, 0, 0);
				renderTarget.FillRectangle(m_darkBrush, dstRect);
			}
		}

		void DrawColoredTile(RenderTarget renderTarget, int tileSize, ref RectF dstRect, SymbolID symbolID, GameColor color, float opacity)
		{
			Dictionary<GameColor, Bitmap> dict;

			if (m_colorTileArray == null)
				m_colorTileArray = new Dictionary<GameColor, Bitmap>[m_bitmapCache.NumDistinctBitmaps];

			dict = m_colorTileArray[(int)symbolID];

			if (dict == null)
			{
				dict = new Dictionary<GameColor, Bitmap>();
				m_colorTileArray[(int)symbolID] = dict;
			}

			Bitmap bitmap;
			if (dict.TryGetValue(color, out bitmap) == false)
			{
				bitmap = new Bitmap(renderTarget, new System.Drawing.Size(tileSize, tileSize),
					new BitmapProperties() { PixelFormat = new PixelFormat(DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), HorizontalDpi = 96, VerticalDpi = 96, });

				const int bytesPerPixel = 4;
				var arr = new byte[tileSize * tileSize * bytesPerPixel];

				var origBmp = m_bitmapCache.GetBitmap(symbolID, color);
				origBmp.CopyPixels(arr, tileSize * 4, 0);
				bitmap.FromMemory(arr, tileSize * 4);
				dict[color] = bitmap;
			}

			renderTarget.DrawBitmap(bitmap, dstRect, opacity, InterpolationMode.Linear);
		}

		private void DrawUncoloredTile(RenderTarget renderTarget, int tileSize, ref RectF dstRect, SymbolID symbolID, float opacity)
		{
			var srcX = (int)symbolID * tileSize;

			renderTarget.DrawBitmap(m_atlasBitmap, dstRect, opacity, InterpolationMode.Linear, new RectF(srcX, 0, tileSize, tileSize));
		}

		#region IDispobable
		bool m_disposed;

		~RendererDetailed()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Dispose unmanaged resources

				if (m_atlasBitmap != null)
				{
					m_atlasBitmap.Dispose();
					m_atlasBitmap = null;
				}

				ClearColorTileArray();

				if (m_bgBrush != null)
				{
					m_bgBrush.Dispose();
					m_bgBrush = null;
				}

				if (m_darkBrush != null)
				{
					m_darkBrush.Dispose();
					m_darkBrush = null;
				}

				m_disposed = true;
			}
		}
		#endregion
	}
}
