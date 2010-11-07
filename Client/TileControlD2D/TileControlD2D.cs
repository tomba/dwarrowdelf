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

		IntPoint m_renderOffset;

		bool m_invalidateRender;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD2D");

		public event Action TileArrangementChanged;

		public TileControlD2D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D2DD3DImage();

			var image = new Image();
			image.Source = m_interopImageSource;
			image.Stretch = System.Windows.Media.Stretch.None;
			image.HorizontalAlignment = HorizontalAlignment.Left;
			image.VerticalAlignment = VerticalAlignment.Top;
			this.Content = image;

			this.Loaded += new RoutedEventHandler(OnLoaded);
			this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			trace.TraceInformation("OnLoaded");

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			// for some reason OnLoaded is called twice
			if (m_d2dFactory != null)
				return;

			m_d2dFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);
#if DEBUG_TEXT
			dwriteFactory = DWriteFactory.CreateFactory();
			textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 10, DWrite.FontWeight.Normal, DWrite.FontStyle.Normal, DWrite.FontStretch.Normal);
#endif
			Window window = Window.GetWindow(this);

			m_interopImageSource.HWNDOwner = (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
			m_interopImageSource.OnRender = this.DoRenderCallback;

			m_invalidateRender = true;
		}

		void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			trace.TraceInformation("OnSizeChanged({0})", e.NewSize);

			UpdateSizes(e.NewSize, m_tileSize);
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceInformation("OnRender");

			if (m_invalidateRender)
			{
				m_interopImageSource.Lock();
				m_interopImageSource.RequestRender();
				m_interopImageSource.Unlock();
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

				trace.TraceInformation("TileSize = {0}", value);

				m_tileSize = value;

				m_renderer.TileSizeChanged(value);

				UpdateSizes(this.RenderSize, m_tileSize);
			}
		}


		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p += new Vector(m_renderOffset.X, m_renderOffset.Y);
			return p;
		}

		/// <summary>
		/// When TileControl size changes or TileSize changes
		/// </summary>
		void UpdateSizes(Size viewSize, int tileSize)
		{
			var columns = (int)Math.Ceiling(viewSize.Width / tileSize) | 1;
			var rows = (int)Math.Ceiling(viewSize.Height / tileSize) | 1;

			bool invalidate = false;

			if (columns != m_columns || rows != m_rows)
			{
				trace.TraceInformation("Columns x Rows = {0}x{1}", columns, rows);

				m_columns = columns;
				m_rows = rows;

				invalidate = true;
			}

			var renderWidth = (int)Math.Ceiling(viewSize.Width);
			var renderHeight = (int)Math.Ceiling(viewSize.Height);

			var renderOffsetX = (int)(renderWidth - tileSize * columns) / 2;
			var renderOffsetY = (int)(renderHeight - tileSize * rows) / 2;
			var renderOffset = new IntPoint(renderOffsetX, renderOffsetY);

			if (renderOffset != m_renderOffset)
			{
				trace.TraceInformation("RenderOffset {0}", m_renderOffset);

				m_renderOffset = renderOffset;

				invalidate = true;
			}

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				SetPixelSize(renderWidth, renderHeight);
			else if (invalidate)
				InvalidateRender();

			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		void SetPixelSize(int width, int height)
		{
			trace.TraceInformation("SetPixelSize({0}, {1})", width, height);

			m_interopImageSource.Lock();
			// implicit render
			m_interopImageSource.SetPixelSize((uint)width, (uint)height);
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
			trace.TraceInformation("DoRender");

			m_invalidateRender = false;

			if (pIDXGISurface != m_pIDXGISurfacePreviousNoRef)
			{
				trace.TraceInformation("Create Render Target");

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

			m_renderTarget.Transform = Matrix3x2F.Translation(m_renderOffset.X, m_renderOffset.Y);

			m_renderer.Render(m_renderTarget, m_columns, m_rows, m_tileSize);

			m_renderTarget.Transform = Matrix3x2F.Identity;

			m_renderTarget.EndDraw();
		}
	}
}
