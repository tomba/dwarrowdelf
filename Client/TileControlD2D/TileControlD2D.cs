//#define DEBUG_TEXT

using System;
using System.Windows;

using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace Dwarrowdelf.Client.TileControl
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD2D : FrameworkElement, ITileControl
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

		Point m_centerPos;
		IntSize m_gridSize;
		double m_tileSize;
		Point m_renderOffset;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD2D");

		bool m_tileLayoutInvalid;
		bool m_tileRenderInvalid;

		public event Action<IntSize, Point> TileLayoutChanged;
		public event Action AboutToRender;

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



		public double TileSize
		{
			get { return m_tileSize; }
			set
			{
				if (value == m_tileSize)
					return;

				trace.TraceInformation("TileSize = {0}", value);

				m_tileSize = value;

				if (m_renderer != null)
					m_renderer.TileSizeChanged((int)value);

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
					TileLayoutChanged(m_gridSize, m_centerPos);

				m_tileLayoutInvalid = false;
			}

			if (m_tileRenderInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender();

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

		public Point CenterPos
		{
			get { return m_centerPos; }

			set
			{
				m_centerPos = value;
				InvalidateTileData();
			}
		}

		Vector ScreenMapDiff { get { return new Vector(Math.Round(this.CenterPos.X), Math.Round(this.CenterPos.Y)); } }

		public Point MapLocationToScreenLocation(Point ml)
		{
			var gridSize = this.GridSize;

			var p = ml - this.ScreenMapDiff;
			p = new Point(p.X, -p.Y);
			p += new Vector(gridSize.Width / 2, gridSize.Height / 2);

			return p;
		}

		public Point ScreenLocationToMapLocation(Point sl)
		{
			var gridSize = this.GridSize;

			var v = sl - new Vector(gridSize.Width / 2, gridSize.Height / 2);
			v = new Point(v.X, -v.Y);
			v += this.ScreenMapDiff;

			return v;
		}

		public Point ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(Point ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public Point ScreenPointToScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new Point(p.X / this.TileSize - 0.5, p.Y / this.TileSize - 0.5);
		}

		public Point ScreenLocationToScreenPoint(Point loc)
		{
			loc.Offset(0.5, 0.5);
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p += new Vector(m_renderOffset.X, m_renderOffset.Y);
			return p;
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
			m_renderOffset = new Point(renderOffsetX, renderOffsetY);

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

		public void InvalidateTileData()
		{
			InvalidateTileRender();
		}

		public void InvalidateSymbols()
		{
			m_renderer.InvalidateSymbols();
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
				System.Diagnostics.Trace.WriteLine(e.ToString());
				System.Diagnostics.Trace.Assert(false);
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

			m_renderTarget.Transform = Matrix3x2F.Translation((float)m_renderOffset.X, (float)m_renderOffset.Y);

			m_renderer.Render(m_renderTarget, m_gridSize.Width, m_gridSize.Height, (int)m_tileSize);

			m_renderTarget.Transform = Matrix3x2F.Identity;

			m_renderTarget.EndDraw();
		}

		#region IDispobable
		public void Dispose()
		{
		}
		#endregion
	}
}
