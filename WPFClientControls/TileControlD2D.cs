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
	public struct MapD2DData
	{
		public byte SymbolID;
		public bool Dark;
		public System.Windows.Media.Color Color;
	}

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

		const int TileZ = 4;
		MapD2DData[, ,] m_tileMap;
		public MapD2DData[, ,] TileMap { get { return m_tileMap; } }

		int m_columns;
		int m_rows;

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		uint m_tileSize;
		IBitmapGenerator m_bitmapGenerator;

		Dictionary<System.Windows.Media.Color, D2DBitmap>[] m_colorTileArray;

		public event Action TileMapChanged;

		public TileControlD2D()
		{
			m_interopImage = new D2DD3DImage();

			var img = new Image();
			img.Stretch = System.Windows.Media.Stretch.None;
			img.Source = m_interopImage;
			this.Content = img;

			this.Loaded += OnLoaded;
			this.SizeChanged += OnSizeChanged;
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

			m_interopImage.RequestRender();
		}

		void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateTileMapSize();
		}

		public void Render()
		{
			m_interopImage.RequestRender();
		}

		public int TileSize
		{
			get { return (int)m_tileSize; }
			set
			{
				m_tileSize = (uint)value;

				m_tileMap = null;
				m_atlasBitmap = null;
				m_colorTileArray = null;

				UpdateTileMapSize();
			}
		}

		public IBitmapGenerator BitmapGenerator
		{
			get { return m_bitmapGenerator; }
			set
			{
				m_bitmapGenerator = value;

				m_atlasBitmap = null;
				m_colorTileArray = null;

				m_interopImage.RequestRender();
			}
		}

		void UpdateTileMapSize()
		{
			if (m_tileSize == 0)
			{
				m_tileMap = null;
				m_interopImage.SetPixelSize(0, 0);
				return;
			}

			int width = (int)Math.Ceiling(this.ActualWidth);
			int height = (int)Math.Ceiling(this.ActualHeight);

			int columns = MyMath.IntDivRound(width, (int)m_tileSize);
			int rows = MyMath.IntDivRound(height, (int)m_tileSize);

			if (m_tileMap == null || (columns != m_columns || rows != m_rows))
			{
				m_columns = columns;
				m_rows = rows;
				m_tileMap = new MapD2DData[m_rows, m_columns, TileZ];
				m_interopImage.SetPixelSize((uint)(m_columns * m_tileSize), (uint)(m_rows * m_tileSize));

				if (TileMapChanged != null)
					TileMapChanged();
			}
		}

		void CreateAtlas()
		{
			var numTiles = m_bitmapGenerator.NumDistinctBitmaps;

			var tileSize = m_tileSize;
			m_atlasBitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize * (uint)numTiles, tileSize),
				new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

			const int bytesPerPixel = 4;
			var arr = new byte[tileSize * tileSize * bytesPerPixel];

			for (uint x = 0; x < numTiles; ++x)
			{
				var bmp = m_bitmapGenerator.GetBitmap((SymbolID)x, System.Windows.Media.Colors.Black, false);
				bmp.CopyPixels(arr, (int)tileSize * 4, 0);
				m_atlasBitmap.CopyFromMemory(new RectU(x * tileSize, 0, x * tileSize + tileSize, tileSize), arr, tileSize * 4);
			}
		}

		void DoRender(IntPtr pIDXGISurface)
		{
			MyDebug.WriteLine("DoRender");

			if (pIDXGISurface != m_pIDXGISurfacePreviousNoRef)
			{
				MyDebug.WriteLine("Create Render Target");

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

				m_atlasBitmap = null;
				m_colorTileArray = null;
			}

			m_renderTarget.BeginDraw();

			m_renderTarget.Clear(new ColorF(0, 0, 0, 1));

			if (m_tileSize == 0 || m_bitmapGenerator == null)
			{
				m_renderTarget.EndDraw();
				return;
			}

			if (m_atlasBitmap == null)
				CreateAtlas();

			var tileSize = m_tileSize;
			SolidColorBrush blackBrush = m_renderTarget.CreateSolidColorBrush(new ColorF(0, 0, 0, 1));
			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var dstRect = new RectF(x * tileSize, y * tileSize, x * tileSize + tileSize, y * tileSize + tileSize);

					for (int z = 0; z < TileZ; ++z)
					{
						MapD2DData data;

						data = m_tileMap[y, x, z];

						byte tileNum = data.SymbolID;

						if (tileNum == 0)
							continue;

						bool dark = data.Dark;
						float opacity = dark ? 0.2f : 1.0f;

						if (data.Color != System.Windows.Media.Colors.Black)
						{
							Dictionary<System.Windows.Media.Color, D2DBitmap> dict;

							if (m_colorTileArray == null)
								m_colorTileArray = new Dictionary<System.Windows.Media.Color, D2DBitmap>[m_bitmapGenerator.NumDistinctBitmaps];

							dict = m_colorTileArray[data.SymbolID];

							if (dict == null)
							{
								dict = new Dictionary<System.Windows.Media.Color, D2DBitmap>();
								m_colorTileArray[data.SymbolID] = dict;
							}

							D2DBitmap bitmap;
							if (dict.TryGetValue(data.Color, out bitmap) == false)
							{
								bitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize, tileSize),
									new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

								const int bytesPerPixel = 4;
								var arr = new byte[tileSize * tileSize * bytesPerPixel];

								var origBmp = m_bitmapGenerator.GetBitmap((SymbolID)data.SymbolID, data.Color, false);
								origBmp.CopyPixels(arr, (int)tileSize * 4, 0);
								bitmap.CopyFromMemory(new RectU(0, 0, tileSize, tileSize), arr, tileSize * 4);
							}

							m_renderTarget.DrawBitmap(bitmap, opacity, BitmapInterpolationMode.Linear, dstRect);
						}
						else
						{
							uint xx = (uint)(tileNum * tileSize);

							m_renderTarget.DrawBitmap(m_atlasBitmap, opacity, BitmapInterpolationMode.Linear,
								dstRect, new RectF(xx, 0, xx + tileSize, tileSize));
						}
					}
#if DEBUG_TEXT
					m_renderTarget.DrawText(String.Format("{0},{1}", x, y), textFormat, dstRect, blackBrush);
#endif
				}
			}

			m_renderTarget.DrawLine(new Point2F(50, 0), new Point2F(0, 50), blackBrush, 2);

			m_renderTarget.EndDraw();
		}
	}
}
