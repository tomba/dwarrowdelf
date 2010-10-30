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

		int m_columns;
		int m_rows;

		// XXX REMVOE??
		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		int m_tileSize;

		Point m_offset;

		bool m_invalidateRender;

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
				m_interopImageSource.Lock();
				m_interopImageSource.RequestRender();
				m_interopImageSource.Unlock();
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

				m_renderer.TileSizeChanged(value);

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
				m_renderer.SizeChanged();
				return true;
			}
			else
			{
				return false;
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


		IRenderer m_renderer;
		public IRenderer Renderer
		{
			set
			{
				if (m_renderer == value)
					return;

				m_renderer = value;
				InvalidateRender();
			}
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
				//Debug.WriteLine("Create Render Target");

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

				m_renderer.RenderTargetChanged();
			}

			m_renderTarget.BeginDraw();

			m_renderTarget.Clear(new ColorF(0, 0, 0, 1));

			if (m_tileSize == 0)
			{
				m_renderTarget.EndDraw();
				return;
			}

			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;

			var dx = (float)(m_renderTarget.PixelSize.Width - m_tileSize * m_columns) / 2;
			var dy = (float)(m_renderTarget.PixelSize.Height - m_tileSize * m_rows) / 2;
			m_renderTarget.Transform = Matrix3x2F.Translation(dx, dy);

			m_renderer.Render(m_renderTarget, m_columns, m_rows, m_tileSize);

			m_renderTarget.Transform = Matrix3x2F.Identity;

			m_renderTarget.EndDraw();
		}
	}
}
