//#define DEBUG_TEXT

using System;
using System.Windows;

using SlimDX;
using SlimDX.Direct2D;
using D3D10 = SlimDX.Direct3D10;
using Device = SlimDX.Direct3D10_1.Device1;
using DXGI = SlimDX.DXGI;

namespace Dwarrowdelf.Client.TileControl
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD2D : FrameworkElement, ITileControl
	{
		Factory m_d2dFactory;
		RenderTarget m_renderTarget;
#if DEBUG_TEXT
		TextFormat textFormat;
		DWriteFactory dwriteFactory;
#endif
		D3D10ImageSlimDX m_interopImageSource;

		Point m_centerPos;
		IntSize m_gridSize;
		double m_tileSize;
		Point m_renderOffset;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD2D");

		bool m_tileLayoutInvalid;
		bool m_tileDataInvalid;
		bool m_tileRenderInvalid;

		public event Action<IntSize, Point> TileLayoutChanged;
		public event Action AboutToRender;

		RendererDetailed m_renderer;
		ISymbolDrawingCache m_symbolDrawingCache;

		Device m_device;
		D3D10.Texture2D m_renderTexture;

		public TileControlD2D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D3D10ImageSlimDX();

			this.Loaded += new RoutedEventHandler(OnLoaded);

			m_device = Helpers10.CreateDevice();
			m_d2dFactory = new Factory(FactoryType.SingleThreaded);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			trace.TraceInformation("OnLoaded");

#if DEBUG_TEXT
			dwriteFactory = DWriteFactory.CreateFactory();
			textFormat = dwriteFactory.CreateTextFormat("Bodoni MT", 10, DWrite.FontWeight.Normal, DWrite.FontStyle.Normal, DWrite.FontStretch.Normal);
#endif

		}

		void InitTextureRenderSurface(int width, int height)
		{
			if (m_renderTexture != null)
			{
				m_interopImageSource.SetBackBufferSlimDX(null);
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			if (m_renderTarget != null)
			{
				m_renderTarget.Dispose();
				m_renderTarget = null;
			}

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);
			m_renderTexture = Helpers10.CreateTextureRenderSurface(m_device, width, height);

			m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);

			RenderTargetProperties rtProperties = new RenderTargetProperties()
			{
				Type = RenderTargetType.Default,
				PixelFormat = new PixelFormat(DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
				HorizontalDpi = m_d2dFactory.DesktopDpi.Width,
				VerticalDpi = m_d2dFactory.DesktopDpi.Height,
			};

			using (var surface = m_renderTexture.AsSurface())
				m_renderTarget = RenderTarget.FromDXGI(m_d2dFactory, surface, rtProperties);

			m_renderer.RenderTargetChanged();
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

		public Point CenterPos
		{
			get { return m_centerPos; }

			set
			{
				m_centerPos = value;

				InvalidateTileData();

				UpdateTileLayout(this.RenderSize);
			}
		}

		public IntSize GridSize
		{
			get { return m_gridSize; }
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (m_disposed)
				return base.ArrangeOverride(arrangeBounds);

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
			if (m_disposed)
				return;
			
			trace.TraceInformation("OnRender");

			var renderSize = this.RenderSize;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(m_gridSize, m_centerPos);
			}

			if (m_tileDataInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender();
			}

			m_interopImageSource.Lock();

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				InitTextureRenderSurface(renderWidth, renderHeight);

			if (m_tileRenderInvalid)
			{
				DoRender();
				m_device.Flush();
			}

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new System.Windows.Rect(renderSize));

			m_tileLayoutInvalid = false;
			m_tileDataInvalid = false;
			m_tileRenderInvalid = false;

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
			if (renderData is RenderData<RenderTileDetailed>)
				m_renderer = new RendererDetailed((RenderData<RenderTileDetailed>)renderData);
			else
				throw new NotSupportedException();

			InvalidateTileData();
		}

		public void InvalidateTileData()
		{
			if (m_tileDataInvalid == false)
			{
				m_tileDataInvalid = true;
				InvalidateTileRender();
			}
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

		void DoRender()
		{
			if (m_renderTarget == null)
				return;

			trace.TraceInformation("DoRender");

			m_renderTarget.BeginDraw();

			m_renderTarget.Clear(new Color4(1.0f, 0, 0, 0));

			if (m_tileSize == 0)
			{
				m_renderTarget.EndDraw();
				return;
			}

			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;

			var m = Matrix3x2.Identity;
			m.M31 = (float)m_renderOffset.X;
			m.M32 = (float)m_renderOffset.Y;
			m_renderTarget.Transform = m;

			m_renderer.Render(m_renderTarget, m_gridSize.Width, m_gridSize.Height, (int)m_tileSize);

			m_renderTarget.Transform = Matrix3x2.Identity;

			m_renderTarget.EndDraw();
		}

		#region IDispobable
		bool m_disposed;

		~TileControlD2D()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Dispose unmanaged resources

				if (m_interopImageSource != null)
				{
					m_interopImageSource.Dispose();
					m_interopImageSource = null;
				}

				if (m_renderTexture != null)
				{
					m_renderTexture.Dispose();
					m_renderTexture = null;
				}

				if (m_renderTarget != null)
				{
					m_renderTarget.Dispose();
					m_renderTarget = null;
				}

				if (m_renderer != null)
				{
					m_renderer.Dispose();
					m_renderer = null;
				}

				if (m_d2dFactory != null)
				{
					m_d2dFactory.Dispose();
					m_d2dFactory = null;
				}

				if (m_device != null)
				{
					m_device.Dispose();
					m_device = null;
				}

				m_disposed = true;
			}
		}
		#endregion
	}
}
