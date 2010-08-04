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

namespace MyGame.Client
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public partial class TileControlD2D : UserControl
	{
		D2DFactory m_d2dFactory;
		RenderTarget m_renderTarget;
#if DEBUG_TEXT
		TextFormat textFormat;
		DWriteFactory dwriteFactory;
#endif
		// Maintained simply to detect changes in the interop back buffer
		IntPtr m_pIDXGISurfacePreviousNoRef;

		D2DD3DImage m_interopImage;

		D2DBitmap m_atlasBitmap;

		IBitmapGenerator m_bitmapGenerator;
		Dictionary<GameColor, D2DBitmap>[] m_colorTileArray;

		RenderMap m_renderMap;
		public RenderMap RenderMap { set { m_renderMap = value; } }

		int m_columns;
		int m_rows;

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		int m_tileSize;

		public delegate void AboutToRenderDelegate(bool arrangementChanged);
		public event AboutToRenderDelegate AboutToRender;

		IntVector m_offset;
		bool m_arrangementChanged;

		uint[] m_simpleBitmapArray;

		SolidColorBrush m_bgBrush;

		const int MINDETAILEDTILESIZE = 8;

		public TileControlD2D()
		{
			m_interopImage = new D2DD3DImage();

			var img = new Image();
			img.Stretch = System.Windows.Media.Stretch.None;
			img.Source = m_interopImage;
			this.Content = img;

			this.Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			m_d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);
#if DEBUG_TEXT
			dwriteFactory = DWriteFactory.CreateFactory();
			textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 10, DWrite.FontWeight.Normal, DWrite.FontStyle.Normal, DWrite.FontStretch.Normal);
#endif
			Window window = Window.GetWindow(this);

			m_interopImage.HWNDOwner = (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
			m_interopImage.OnRender = this.DoRender;

			// Need an explicit render first?
			m_interopImage.RequestRender();
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

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			var pw = (uint)Math.Ceiling(sizeInfo.NewSize.Width);
			var ph = (uint)Math.Ceiling(sizeInfo.NewSize.Height);
			/* Allocate some extra, so that we don't need to re-allocate the surface for every tilesize change */
			pw = (pw | 0xff) + 1;
			ph = (ph | 0xff) + 1;

			if (m_interopImage.PixelWidth != pw || m_interopImage.PixelHeight != ph)
			{
				m_simpleBitmapArray = null;
				m_interopImage.Lock();
				// implicit render
				m_interopImage.SetPixelSize(pw, ph);
				m_interopImage.Unlock();
			}

			UpdateTileMapSize();

			base.OnRenderSizeChanged(sizeInfo);
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

		public int TileSize
		{
			get { return m_tileSize; }
			set
			{
				m_tileSize = value;

				ClearAtlasBitmap();
				ClearColorTileArray();

				UpdateTileMapSize();
			}
		}

		public void InvalidateBitmaps()
		{
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

				InvalidateArrange();
			}
		}

		void UpdateOffset(Size size, int tileSize)
		{
			var dx = ((tileSize * m_columns) - (int)Math.Ceiling(size.Width)) / 2;
			var dy = ((tileSize * m_rows) - (int)Math.Ceiling(size.Height)) / 2;
			m_offset = new IntVector(dx, dy);
		}

		void UpdateTileMapSize()
		{
			//MyDebug.WriteLine("UpdateTileMapSize");

			if (m_tileSize == 0)
			{
				m_simpleBitmapArray = null;
				m_interopImage.SetPixelSize(0, 0);
				return;
			}

			int newColumns = MyMath.IntDivRound((int)Math.Ceiling(this.ActualWidth), m_tileSize) | 1;
			int newRows = MyMath.IntDivRound((int)Math.Ceiling(this.ActualHeight), m_tileSize) | 1;

			if (newColumns != m_columns || newRows != m_rows)
			{
				m_columns = newColumns;
				m_rows = newRows;
			}

			UpdateOffset(this.RenderSize, m_tileSize);

			m_arrangementChanged = true;

			InvalidateArrange();
		}

		void CreateAtlas()
		{
			//MyDebug.WriteLine("CreateAtlas");

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

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			//MyDebug.WriteLine("Arrange");

			if (AboutToRender != null)
				AboutToRender(m_arrangementChanged);

			m_arrangementChanged = false;

			m_interopImage.RequestRender();

			return base.ArrangeOverride(arrangeBounds);
		}

		void DoRender(IntPtr pIDXGISurface)
		{
			//MyDebug.WriteLine("DoRender");

			if (pIDXGISurface != m_pIDXGISurfacePreviousNoRef)
			{
				//MyDebug.WriteLine("Create Render Target");

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
			}

			DoRenderTiles();
		}

		void DoRenderTiles()
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

			if (m_tileSize > MINDETAILEDTILESIZE)
				RenderDetailedTiles(m_tileSize);
			else
				RenderSimpleTiles(m_tileSize);

			m_renderTarget.EndDraw();
		}

		unsafe void RenderSimpleTiles(int tileSize)
		{
			uint bytespp = 4;
			uint w = (uint)(m_interopImage.PixelWidth);
			uint h = (uint)(m_interopImage.PixelHeight);

			if (m_simpleBitmapArray == null)
				m_simpleBitmapArray = new uint[w * h];

			fixed (uint* a = m_simpleBitmapArray)
			{
				for (int y = 0; y < m_rows; ++y)
				{
					for (int x = 0; x < m_columns; ++x)
					{
						RenderTile data = m_renderMap.ArrayGrid.Grid[y, x];
						var rgb = new GameColorRGB(data.Color);
						uint c = (uint)((rgb.R << 16) | (rgb.G << 8) | (rgb.B << 0));

						for (int ty = 0; ty < tileSize; ++ty)
							for (int tx = 0; tx < tileSize; ++tx)
								a[((m_rows - y - 1) * tileSize + ty) * w + (x * tileSize + tx)] = c;
					}
				}

				var bmp = m_renderTarget.CreateBitmap(new SizeU(w, h), (IntPtr)a, w * bytespp,
					new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Ignore), 96, 96));
				m_renderTarget.DrawBitmap(bmp, 1.0f, BitmapInterpolationMode.Linear, new RectF(-m_offset.X, -m_offset.Y, w - m_offset.X, h - m_offset.Y));
				bmp.Dispose();
			}
		}

		void RenderDetailedTiles(int tileSize)
		{
#if DEBUG_TEXT
			var blackBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(0, 0, 0, 1));
#endif

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var x1 = x * tileSize - m_offset.X;
					var y1 = (m_rows - y - 1) * tileSize - m_offset.Y;
					var dstRect = new RectF(x1, y1, x1 + tileSize, y1 + tileSize);

					RenderTile data = m_renderMap.ArrayGrid.Grid[y, x];

					if (data.FloorSymbolID != SymbolID.Undefined)
						DrawTile(tileSize, ref dstRect, data.FloorSymbolID, data.FloorColor, data.FloorBgColor, data.FloorDark);

					if (data.InteriorSymbolID != SymbolID.Undefined)
						DrawTile(tileSize, ref dstRect, data.InteriorSymbolID, data.InteriorColor, data.InteriorBgColor, data.InteriorDark);

					if (data.ObjectSymbolID != SymbolID.Undefined)
						DrawTile(tileSize, ref dstRect, data.ObjectSymbolID, data.ObjectColor, data.ObjectBgColor, data.ObjectDark);

					if (data.TopSymbolID != SymbolID.Undefined)
						DrawTile(tileSize, ref dstRect, data.TopSymbolID, data.TopColor, data.TopBgColor, data.TopDark);
#if DEBUG_TEXT
					m_renderTarget.DrawText(String.Format("{0},{1}", x, y), textFormat, dstRect, blackBrush);
#endif
				}
			}
		}

		void DrawTile(int tileSize, ref RectF dstRect, SymbolID symbolID, GameColor color, GameColor bgColor, bool dark)
		{
			// XXX should this be cleared when renderTarget changes?
			if (m_bgBrush == null)
				m_bgBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(0, 0, 0, 1));

			if (bgColor != GameColor.None)
			{
				var rgb = new GameColorRGB(bgColor);
				m_bgBrush.Color = new ColorF((float)rgb.R / 255, (float)rgb.G / 255, (float)rgb.B / 255, 1.0f);
				m_renderTarget.FillRectangle(dstRect, m_bgBrush);
			}

			if (color != GameColor.None)
				DrawColoredTile(tileSize, ref dstRect, symbolID, color, 1.0f);
			else
				DrawUncoloredTile(tileSize, ref dstRect, symbolID, 1.0f);

			if (dark)
			{
				m_bgBrush.Color = new ColorF(0, 0, 0, 0.8f);
				m_renderTarget.FillRectangle(dstRect, m_bgBrush);
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
