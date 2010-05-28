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
		// color?
	}

	public partial class MapControlD2D : UserControl
	{
		D2DFactory m_d2dFactory;
		RenderTarget m_renderTarget;
#if DEBUG_TEXT
		TextFormat textFormat;
		DWriteFactory dwriteFactory;
#endif
		// Maintained simply to detect changes in the interop back buffer
		IntPtr m_pIDXGISurfacePreviousNoRef;

		D2DBitmap m_atlasBitmap;

		const int TileZ = 4;

		MapD2DData[, ,] m_tileMap;
		public MapD2DData[, ,] TileMap { get { return m_tileMap; } }

		int m_columns;
		int m_rows;

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		uint m_tileSize;
		System.Windows.Media.Imaging.BitmapSource[] m_bitmapArray;

		D2DD3DImage m_interopImage;

		public MapControlD2D()
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
			UpdateD2DSize();
			UpdateTileMapSize();
		}

		public void Render()
		{
			m_interopImage.RequestRender();
		}

		public void SetSymbolBitmaps(System.Windows.Media.Imaging.BitmapSource[] bitmapArray, int tileSize)
		{
			m_bitmapArray = bitmapArray;
			m_tileSize = (uint)tileSize;
			UpdateTileMapSize();
			CreateAtlas();
			m_interopImage.RequestRender();
		}

		void UpdateD2DSize()
		{
			uint surfWidth = (uint)Math.Ceiling(this.ActualWidth);
			uint surfHeight = (uint)Math.Ceiling(this.ActualHeight);

			m_interopImage.SetPixelSize(surfWidth, surfHeight);
		}

		void UpdateTileMapSize()
		{
			if (m_tileSize == 0)
			{
				m_tileMap = null;
				return;
			}

			int width = m_interopImage.PixelWidth;
			int height = m_interopImage.PixelHeight;

			int columns = MyMath.IntDivRound(width, (int)m_tileSize);
			int rows = MyMath.IntDivRound(height, (int)m_tileSize);

			if (columns != m_columns || rows != m_rows)
			{
				m_columns = columns;
				m_rows = rows;
				m_tileMap = new MapD2DData[m_rows, m_columns, TileZ];
			}
		}

		void CreateAtlas()
		{
			if (m_renderTarget == null)
				return;

			if (m_bitmapArray == null)
				return;

			var tileSize = m_tileSize;
			m_atlasBitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize * (uint)m_bitmapArray.Length, tileSize),
				new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

			const int bytesPerPixel = 4;
			var arr = new byte[tileSize * tileSize * bytesPerPixel];

			for (uint x = 0; x < m_bitmapArray.Length; ++x)
			{
				var bmp = m_bitmapArray[x];
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

				CreateAtlas();
			}

			m_renderTarget.BeginDraw();

			m_renderTarget.Clear(new ColorF(0, 0, 0, 1));

			if (m_tileSize == 0 || m_atlasBitmap == null)
			{
				m_renderTarget.EndDraw();
				return;
			}

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
						byte tileNum = m_tileMap[y, x, z].SymbolID;

						if (tileNum == 0)
							continue;

						bool dark = m_tileMap[y, x, z].Dark;

						uint xx = (uint)(tileNum * tileSize);

						float opacity = dark ? 0.2f : 1.0f;

						m_renderTarget.DrawBitmap(m_atlasBitmap, opacity, BitmapInterpolationMode.Linear,
							dstRect, new RectF(xx, 0, xx + tileSize, tileSize));
					}
#if DEBUG_TEXT
					m_renderTarget.DrawText(String.Format("{0},{1}", x, y), textFormat, dstRect, blackBrush);
#endif
				}
			}

			m_renderTarget.EndDraw();
		}
	}
}
