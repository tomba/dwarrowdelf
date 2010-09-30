//#define DEBUG_TEXT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using System.Diagnostics;

namespace Dwarrowdelf.Client.TileControlD2D
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD2D : UserControl
	{
		D2DFactory m_d2dFactory;
		RenderTarget m_renderTarget;
#if DEBUG_TEXT
		TextFormat textFormat;
		DWriteFactory dwriteFactory;
#endif
		// Maintained simply to detect changes in the interop back buffer
		IntPtr m_pIDXGISurfacePreviousNoRef;

		D2DD3DImage m_interopImageSource;

		D2DBitmap m_atlasBitmap;

		IBitmapGenerator m_bitmapGenerator;
		Dictionary<GameColor, D2DBitmap>[] m_colorTileArray;

		int m_columns;
		int m_rows;

		// XXX REMVOE??
		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		int m_tileSize;

		Point m_offset;

		uint[] m_simpleBitmapArray;

		SolidColorBrush m_bgBrush;
		SolidColorBrush m_darkBrush;

		bool m_invalidateRender;

		const int MINDETAILEDTILESIZE = 8;

		public TileControlD2D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D2DD3DImage();

			var image = new Image();
			image.Source = m_interopImageSource;
			image.Stretch = System.Windows.Media.Stretch.None;
			image.HorizontalAlignment = HorizontalAlignment.Center;
			image.VerticalAlignment = VerticalAlignment.Center;
			this.Content = image;

			this.Loaded += new RoutedEventHandler(OnLoaded);
			this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			//Debug.WriteLine("OnLoaded");

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);
#if DEBUG_TEXT
			dwriteFactory = DWriteFactory.CreateFactory();
			textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 10, DWrite.FontWeight.Normal, DWrite.FontStyle.Normal, DWrite.FontStretch.Normal);
#endif
			Window window = Window.GetWindow(this);

			m_interopImageSource.HWNDOwner = (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
			m_interopImageSource.OnRender = this.DoRenderCallback;

			// Need an explicit render first?
			m_interopImageSource.RequestRender();
		}

		void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine("OnSizeChanged({0})", e.NewSize);

			UpdateSizes();

			var pw = (uint)Math.Ceiling(e.NewSize.Width);
			var ph = (uint)Math.Ceiling(e.NewSize.Height);

			/* Allocate some extra, so that we don't need to re-allocate the surface for every tilesize change */
			pw = (pw | 0xff) + 1;
			ph = (ph | 0xff) + 1;

			if (m_interopImageSource != null && (m_interopImageSource.PixelWidth != pw || m_interopImageSource.PixelHeight != ph))
			{
				SetPixelSize(pw, ph);
			}
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			//Debug.WriteLine("OnRender");

			if (m_invalidateRender)
			{
				m_interopImageSource.RequestRender();
				m_invalidateRender = false;
			}
		}

		public void InvalidateRender()
		{
			m_invalidateRender = true;
			InvalidateVisual();
		}

		public int TileSize
		{
			get { return m_tileSize; }
			set
			{
				if (value == m_tileSize)
					return;

				//Debug.WriteLine("TileSize = {0}", value);

				m_tileSize = value;

				ClearAtlasBitmap();
				ClearColorTileArray();

				if (m_tileSize == 0)
				{
					if (m_interopImageSource != null)
						SetPixelSize(0, 0);
					return;
				}

				UpdateSizes();
			}
		}


		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			p += new Vector(m_offset.X, m_offset.Y);
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p -= new Vector(m_offset.X, m_offset.Y);
			return p;
		}


		void UpdateSizes()
		{
			var b = UpdateColumnsAndRows();
			UpdateOffset();

			if (b)
				InvalidateRender();
		}

		bool UpdateOffset()
		{
			var dx = ((m_tileSize * m_columns) - this.RenderSize.Width) / 2;
			var dy = ((m_tileSize * m_rows) - this.RenderSize.Height) / 2;
			
			var newOffset = new Point(dx, dy);

			//Debug.WriteLine("UpdateOffset({0}, {1}) = {2}", this.RenderSize, m_tileSize, newOffset);

			if (m_offset != newOffset)
			{
				m_offset = newOffset;
				return true;
			}
			else
			{
				return false;
			}
		}

		bool UpdateColumnsAndRows()
		{
			var newColumns = (int)Math.Ceiling(this.RenderSize.Width / m_tileSize) | 1;
			var newRows = (int)Math.Ceiling(this.RenderSize.Height / m_tileSize) | 1;

			//Debug.WriteLine("UpdateColumnsAndRows({0}) = {1}, {2}", this.RenderSize, newColumns, newRows);

			if (newColumns != m_columns || newRows != m_rows)
			{
				m_columns = newColumns;
				m_rows = newRows;
				m_simpleBitmapArray = null;
				return true;
			}
			else
			{
				return false;
			}
		}






		void ClearAtlasBitmap()
		{
			if (m_atlasBitmap != null)
				m_atlasBitmap.Dispose();

			m_atlasBitmap = null;
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
			}

			m_colorTileArray = null;
		}

		public void InvalidateBitmaps()
		{
			//Debug.WriteLine("InvalidateBitmaps");

			ClearAtlasBitmap();
			ClearColorTileArray();
		}

		public IBitmapGenerator BitmapGenerator
		{
			get { return m_bitmapGenerator; }
			set
			{
				m_bitmapGenerator = value;

				ClearAtlasBitmap();
				ClearColorTileArray();

				InvalidateRender();
			}
		}
		
		void CreateAtlas()
		{
			//My//Debug.WriteLine("CreateAtlas");

			var numTiles = m_bitmapGenerator.NumDistinctBitmaps;

			var tileSize = (uint)m_tileSize;
			const int bytesPerPixel = 4;
			var arr = new byte[tileSize * tileSize * bytesPerPixel];

			m_atlasBitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize * (uint)numTiles, tileSize),
				new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

			for (uint x = 0; x < numTiles; ++x)
			{
				var bmp = m_bitmapGenerator.GetBitmap((SymbolID)x, GameColor.None);
				bmp.CopyPixels(arr, (int)tileSize * 4, 0);
				m_atlasBitmap.CopyFromMemory(new RectU(x * tileSize, 0, x * tileSize + tileSize, tileSize), arr, tileSize * 4);
			}
		}





		void SetPixelSize(uint width, uint height)
		{
			//Debug.WriteLine("SetPixelSize({0},{1})", width, height);

			m_interopImageSource.Lock();
			// implicit render
			m_interopImageSource.SetPixelSize(width, height);
			m_interopImageSource.Unlock();
		}


		IRenderViewRenderer m_renderView;
		public IRenderViewRenderer RenderView
		{
			set { m_renderView = value; }
		}

		void DoRenderCallback(IntPtr pIDXGISurface)
		{
			try
			{
				DoRender(pIDXGISurface);
			}
			catch (Exception e)
			{
				Trace.WriteLine(e.ToString());
				Trace.Assert(false);
			}
		}

		void DoRender(IntPtr pIDXGISurface)
		{
			//Debug.WriteLine("DoRender");

			if (pIDXGISurface != m_pIDXGISurfacePreviousNoRef)
			{
				//My//Debug.WriteLine("Create Render Target");

				m_pIDXGISurfacePreviousNoRef = pIDXGISurface;

				// Create the render target
				Surface dxgiSurface = Surface.FromNativeSurface(pIDXGISurface);
				SurfaceDescription sd = dxgiSurface.Description;

				RenderTargetProperties rtp =
					new RenderTargetProperties(
						RenderTargetType.Default,
						new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied),
						96,
						96,
						RenderTargetUsage.None,
						Microsoft.WindowsAPICodePack.DirectX.Direct3D.FeatureLevel.Default);

				try
				{
					m_renderTarget = m_d2dFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, rtp);
				}
				catch (Exception)
				{
					return;
				}

				ClearAtlasBitmap();
				ClearColorTileArray();

				if (m_bgBrush != null)
					m_bgBrush.Dispose();
				m_bgBrush = null;

				if (m_darkBrush != null)
					m_darkBrush.Dispose();
				m_darkBrush = null;
			}

			var renderMap = m_renderView.GetRenderMap(m_columns, m_rows);

			DoRenderTiles(renderMap);
		}

		void DoRenderTiles(RenderMap renderMap)
		{
			m_renderTarget.BeginDraw();

			m_renderTarget.Clear(new ColorF(0, 0, 0, 1));

			if (m_tileSize == 0 || m_bitmapGenerator == null)
			{
				m_renderTarget.EndDraw();
				return;
			}

			if (m_atlasBitmap == null)
				CreateAtlas();

			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;

			var dx = (float)(m_renderTarget.PixelSize.Width - m_tileSize * m_columns) / 2;
			var dy = (float)(m_renderTarget.PixelSize.Height - m_tileSize * m_rows) / 2;
			m_renderTarget.Transform = Matrix3x2F.Translation(dx, dy);

			if (m_tileSize > MINDETAILEDTILESIZE)
				RenderDetailedTiles(renderMap, m_tileSize);
			else
				RenderSimpleTiles(renderMap, m_tileSize);

			m_renderTarget.Transform = Matrix3x2F.Identity;

			m_renderTarget.EndDraw();
		}

		unsafe void RenderSimpleTiles(RenderMap renderMap, int tileSize)
		{
			uint bytespp = 4;
			uint w = (uint)m_columns;
			uint h = (uint)m_rows;

			if (m_simpleBitmapArray == null)
				m_simpleBitmapArray = new uint[w * h];

			fixed (uint* a = m_simpleBitmapArray)
			{
				for (int y = 0; y < m_rows && y < renderMap.Size.Height; ++y)
				{
					for (int x = 0; x < m_columns && x < renderMap.Size.Width; ++x)
					{
						RenderTile data = renderMap.ArrayGrid.Grid[y, x];
						var rgb = new GameColorRGB(data.Color);
						var m = 1.0 - data.DarknessLevel / 255.0;
						var r = (byte)(rgb.R * m);
						var g = (byte)(rgb.G * m);
						var b = (byte)(rgb.B * m);
						uint c = (uint)((r << 16) | (g << 8) | (b << 0));

						a[(m_rows - y - 1) * w + x] = c;
					}
				}

				var bmp = m_renderTarget.CreateBitmap(new SizeU(w, h), (IntPtr)a, w * bytespp,
					new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Ignore), 96, 96));
				var destRect = new RectF(0, 0, w * tileSize, h * tileSize);
				m_renderTarget.DrawBitmap(bmp, 1.0f, BitmapInterpolationMode.NearestNeighbor, destRect);
				bmp.Dispose();
			}
		}

		void RenderDetailedTiles(RenderMap renderMap, int tileSize)
		{
#if DEBUG_TEXT
			var blackBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(255, 255, 255, 1));
#endif
			if (m_bgBrush == null)
				m_bgBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(0, 0, 0, 1));

			if (m_darkBrush == null)
				m_darkBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(0, 0, 0, 1));

			for (int y = 0; y < m_rows && y < renderMap.Size.Height; ++y)
			{
				for (int x = 0; x < m_columns && x < renderMap.Size.Width; ++x)
				{
					var x1 = x * tileSize;
					var y1 = (m_rows - y - 1) * tileSize;
					var dstRect = new RectF(x1, y1, x1 + tileSize, y1 + tileSize);

					RenderTile data = renderMap.ArrayGrid.Grid[y, x];

					var d1 = (data.Floor.DarknessLevel - data.Interior.DarknessLevel) / 255f;
					var d2 = (data.Interior.DarknessLevel - data.Object.DarknessLevel) / 255f;
					var d3 = (data.Object.DarknessLevel - data.Top.DarknessLevel) / 255f;
					var d4 = (data.Top.DarknessLevel) / 255f;

					var o4 = d4;
					var o3 = d3 / (1f - d4);
					var o2 = d2 / (1f - (d3 + d4));
					var o1 = d1 / (1f - (d2 + d3 + d4));

					DrawTile(tileSize, ref dstRect, ref data.Floor, o1);
					DrawTile(tileSize, ref dstRect, ref data.Interior, o2);
					DrawTile(tileSize, ref dstRect, ref data.Object, o3);
					DrawTile(tileSize, ref dstRect, ref data.Top, o4);
#if DEBUG_TEXT
					m_renderTarget.DrawRectangle(dstRect, blackBrush, 1);
					m_renderTarget.DrawText(String.Format("{0},{1}", x, y), textFormat, dstRect, blackBrush);
#endif
				}
			}
		}

		void DrawTile(int tileSize, ref RectF dstRect, ref RenderTileLayer tile, float darkOpacity)
		{
			if (tile.SymbolID != SymbolID.Undefined && darkOpacity < 0.99f)
			{
				if (tile.BgColor != GameColor.None)
				{
					var rgb = new GameColorRGB(tile.BgColor);
					m_bgBrush.Color = new ColorF((float)rgb.R / 255, (float)rgb.G / 255, (float)rgb.B / 255, 1.0f);
					m_renderTarget.FillRectangle(dstRect, m_bgBrush);
				}

				if (tile.Color != GameColor.None)
					DrawColoredTile(tileSize, ref dstRect, tile.SymbolID, tile.Color, 1.0f);
				else
					DrawUncoloredTile(tileSize, ref dstRect, tile.SymbolID, 1.0f);
			}

			if (darkOpacity > 0.01f)
			{
				m_darkBrush.Color = new ColorF(0, 0, 0, darkOpacity);
				m_renderTarget.FillRectangle(dstRect, m_darkBrush);
			}
		}

		void DrawColoredTile(int tileSize, ref RectF dstRect, SymbolID symbolID, GameColor color, float opacity)
		{
			Dictionary<GameColor, D2DBitmap> dict;

			if (m_colorTileArray == null)
				m_colorTileArray = new Dictionary<GameColor, D2DBitmap>[m_bitmapGenerator.NumDistinctBitmaps];

			dict = m_colorTileArray[(int)symbolID];

			if (dict == null)
			{
				dict = new Dictionary<GameColor, D2DBitmap>();
				m_colorTileArray[(int)symbolID] = dict;
			}

			D2DBitmap bitmap;
			if (dict.TryGetValue(color, out bitmap) == false)
			{
				bitmap = m_renderTarget.CreateBitmap(new SizeU((uint)tileSize, (uint)tileSize),
					new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

				const int bytesPerPixel = 4;
				var arr = new byte[tileSize * tileSize * bytesPerPixel];

				var origBmp = m_bitmapGenerator.GetBitmap(symbolID, color);
				origBmp.CopyPixels(arr, (int)tileSize * 4, 0);
				bitmap.CopyFromMemory(new RectU(0, 0, (uint)tileSize, (uint)tileSize), arr, (uint)tileSize * 4);
				dict[color] = bitmap;
			}

			m_renderTarget.DrawBitmap(bitmap, opacity, BitmapInterpolationMode.Linear, dstRect);
		}

		private void DrawUncoloredTile(int tileSize, ref RectF dstRect, SymbolID symbolID, float opacity)
		{
			var srcX = (int)symbolID * tileSize;

			m_renderTarget.DrawBitmap(m_atlasBitmap, opacity, BitmapInterpolationMode.Linear,
				dstRect, new RectF(srcX, 0, srcX + tileSize, tileSize));
		}

	}
}
