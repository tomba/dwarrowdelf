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

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;

namespace MyGame.Client
{
	public partial class MapControlD2D : UserControl
	{
		D2DFactory m_d2dFactory;
		RenderTarget m_renderTarget;

		// Maintained simply to detect changes in the interop back buffer
		IntPtr m_pIDXGISurfacePreviousNoRef;

		D2DBitmap m_atlasBitmap;

		const int TileZ = 4;

		byte[, ,] m_tileMap;
		public byte[,,] TileMap { get { return m_tileMap; } }

		int m_columns;
		int m_rows;

		uint m_tileSize;
		System.Windows.Media.Imaging.BitmapSource[] m_bitmapArray;

		public void SetTiles(System.Windows.Media.Imaging.BitmapSource[] bitmapArray, int tileSize)
		{
			m_bitmapArray = bitmapArray;
			m_tileSize = (uint)tileSize;
			UpdateTileMapSize();
			CreateAtlas();
			InteropImage.RequestRender();
		}

		D2DD3DImage InteropImage;

		public MapControlD2D()
		{
			InteropImage = new D2DD3DImage();

			var img = new Image();
			img.Stretch = System.Windows.Media.Stretch.Fill;
			img.Source = InteropImage;
			this.Content = img;

			this.Loaded += new RoutedEventHandler(host_Loaded);
			this.SizeChanged += new SizeChangedEventHandler(host_SizeChanged);
		}

		void host_Loaded(object sender, RoutedEventArgs e)
		{
			m_d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

			//Window window = Application.Current.MainWindow;
			Window window = Window.GetWindow(this);

			InteropImage.HWNDOwner = (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
			InteropImage.OnRender = this.DoRender;

			InteropImage.RequestRender();
		}

		void host_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateD2DSize();
			UpdateTileMapSize();
		}

		void UpdateD2DSize()
		{
			// TODO: handle non-96 DPI
			uint surfWidth = (uint)(this.ActualWidth < 0 ? 0 : Math.Ceiling(this.ActualWidth));
			uint surfHeight = (uint)(this.ActualHeight < 0 ? 0 : Math.Ceiling(this.ActualHeight));

			InteropImage.SetPixelSize(surfWidth, surfHeight);
		}

		void UpdateTileMapSize()
		{
			m_columns = (int)(this.ActualWidth / m_tileSize);
			m_rows = (int)(this.ActualHeight / m_tileSize);

			m_tileMap = new byte[m_rows, m_columns, TileZ];
		}

		void CreateAtlas()
		{
			if (m_renderTarget == null)
				return;

			var tileSize = m_tileSize;
			m_atlasBitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize * (uint)m_bitmapArray.Length, tileSize),
				new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

			var arr = new byte[tileSize * tileSize * 4];

			for (uint x = 0; x < m_bitmapArray.Length; ++x)
			{
				var bmp = m_bitmapArray[x];
				bmp.CopyPixels(arr, (int)tileSize * 4, 0);
				m_atlasBitmap.CopyFromMemory(new RectU(x * tileSize, 0, x * tileSize + tileSize, tileSize), arr, tileSize * 4);
			}
		}

		void DoRender(IntPtr pIDXGISurface)
		{
			if (pIDXGISurface != m_pIDXGISurfacePreviousNoRef)
			{
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

			m_renderTarget.Clear(new ColorF(1, 1, 1, 0));

			var tileSize = m_tileSize;

			// m_renderTarget.PixelSize.Height
			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					for (int z = 0; z < TileZ; ++z)
					{
						byte tileNum = m_tileMap[y, x, z];

						uint xx = (uint)(tileNum * tileSize);

						m_renderTarget.DrawBitmap(m_atlasBitmap, 1, BitmapInterpolationMode.Linear,
							new RectF(x * tileSize, y * tileSize, x * tileSize + tileSize, y * tileSize + tileSize),
							new RectF(xx, 0, xx + tileSize, tileSize));
					}
				}
			}

			m_renderTarget.EndDraw();
		}
	}
}
