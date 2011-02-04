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

namespace Dwarrowdelf.Client.TileControl
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD2D : FrameworkElement /*, ITileControl*/
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

		IntPoint m_centerPos;
		IntSize m_gridSize;
		int m_tileSize;

		IntPoint m_renderOffset;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD2D");

		bool m_tileLayoutInvalid;
		bool m_tileRenderInvalid;

		public event Action<IntSize> TileLayoutChanged;
		public event Action<Size> AboutToRender;

		IRenderer m_renderer;
		ISymbolDrawingCache m_symbolDrawingCache;

		public TileControlD2D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D2DD3DImage();

			this.Loaded += new RoutedEventHandler(OnLoaded);
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

			m_tileLayoutInvalid = true;
			m_tileRenderInvalid = true;

			// This seems to initialize the imagesource...
			m_interopImageSource.SetPixelSize(0, 0);
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

				if (m_renderer != null)
					m_renderer.TileSizeChanged(value);

				UpdateTileLayout(this.RenderSize);
			}
		}

		public IntSize GridSize
		{
			get { return m_gridSize; }
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceInformation("ArrangeOverride({0})", arrangeBounds);

			var renderSize = arrangeBounds;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				UpdateTileLayout(renderSize);

			return base.ArrangeOverride(arrangeBounds);
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceInformation("OnRender");

			var renderSize = this.RenderSize;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(m_gridSize);

				m_tileLayoutInvalid = false;
			}

			if (m_tileRenderInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender(renderSize);

				m_interopImageSource.Lock();

				if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
					m_interopImageSource.SetPixelSize((uint)renderWidth, (uint)renderHeight); // implicit render
				else
					m_interopImageSource.RequestRender();

				m_interopImageSource.Unlock();

				m_tileRenderInvalid = false;
			}

			drawingContext.DrawImage(m_interopImageSource, new System.Windows.Rect(renderSize));

			trace.TraceInformation("OnRender End");
		}

		public void InvalidateTileRender()
		{
			if (m_tileRenderInvalid == false)
			{
				trace.TraceInformation("InvalidateRender");
				m_tileRenderInvalid = true;
				InvalidateVisual();
			}
		}


		public int Columns { get { return this.GridSize.Width; } }
		public int Rows { get { return this.GridSize.Height; } }

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		IntPoint BottomLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, -this.Rows / 2); }
		}

		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set { m_centerPos = value; }
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

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(IntPoint ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X - this.TopLeftPos.X, -(ml.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sl)
		{
			return new IntPoint(sl.X + this.TopLeftPos.X, -(sl.Y - this.TopLeftPos.Y));
		}





		void UpdateTileLayout(Size renderSize)
		{
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			var columns = (int)Math.Ceiling(renderSize.Width / m_tileSize) | 1;
			var rows = (int)Math.Ceiling(renderSize.Height / m_tileSize) | 1;
			m_gridSize = new IntSize(columns, rows);

			var renderOffsetX = (int)(renderWidth - m_tileSize * m_gridSize.Width) / 2;
			var renderOffsetY = (int)(renderHeight - m_tileSize * m_gridSize.Height) / 2;
			m_renderOffset = new IntPoint(renderOffsetX, renderOffsetY);

			m_tileLayoutInvalid = true;

			trace.TraceInformation("UpdateTileLayout({0}, {1}, {2}) -> Off {3}, Grid {4}", renderSize, m_gridSize, m_tileSize,
				m_renderOffset, m_gridSize);

			InvalidateTileRender();
		}

		public void SetRenderData(IRenderData renderData)
		{
			IRenderer renderer;

			if (renderData is RenderData<RenderTileDetailed>)
				renderer = new RendererDetailed((RenderData<RenderTileDetailed>)renderData);
			else
				throw new NotSupportedException();

			m_renderer = renderer;
			InvalidateTileRender();
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;
				m_renderer.SymbolDrawingCache = value;
				InvalidateTileRender();
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

			m_renderer.Render(m_renderTarget, m_gridSize.Width, m_gridSize.Height, m_tileSize);

			m_renderTarget.Transform = Matrix3x2F.Identity;

			m_renderTarget.EndDraw();
		}
	}
}
