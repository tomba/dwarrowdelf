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

		byte[, ,] m_tileMap;

		int m_columns;
		int m_rows;

		int m_tileSize;
		public int TileSize
		{
			get { return m_tileSize; }
			set { m_tileSize = value; UpdateTileMapSize(); }
		}

		public System.Windows.Media.Imaging.BitmapSource[] BitmapArray;


		public MapControlD2D()
		{
			InitializeComponent();

			host.Loaded += new RoutedEventHandler(host_Loaded);
			host.SizeChanged += new SizeChangedEventHandler(host_SizeChanged);
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
			uint surfWidth = (uint)(host.ActualWidth < 0 ? 0 : Math.Ceiling(host.ActualWidth));
			uint surfHeight = (uint)(host.ActualHeight < 0 ? 0 : Math.Ceiling(host.ActualHeight));

			InteropImage.SetPixelSize(surfWidth, surfHeight);
		}

		void UpdateTileMapSize()
		{
			m_columns = (int)(host.ActualWidth / TileSize);
			m_rows = (int)(host.ActualHeight / TileSize);

			m_tileMap = new byte[m_rows, m_columns, 4];
		}

		void CreateAtlas(System.Windows.Media.Imaging.BitmapSource[] bitmapArray, uint tileSize)
		{
			m_atlasBitmap = m_renderTarget.CreateBitmap(new SizeU(tileSize * (uint)bitmapArray.Length, tileSize),
				new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNORM, AlphaMode.Premultiplied), 96, 96));

			var arr = new byte[tileSize * tileSize * 4];

			for (uint x = 0; x < bitmapArray.Length; ++x)
			{
				var bmp = bitmapArray[x];
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

				CreateAtlas(this.BitmapArray, (uint)this.TileSize);
			}

			m_renderTarget.BeginDraw();

			var tileSize = (uint)this.TileSize;

			// m_renderTarget.PixelSize.Height
			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					for (int z = 0; z < 4; ++z)
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
