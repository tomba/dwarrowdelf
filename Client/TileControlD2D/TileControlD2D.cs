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
	public class TileControlD2D : TileControlBase, IDisposable
	{
		Factory m_d2dFactory;
		RenderTarget m_renderTarget;
#if DEBUG_TEXT
		TextFormat textFormat;
		DWriteFactory dwriteFactory;
#endif
		D3D10ImageSlimDX m_interopImageSource;

		RendererDetailed m_renderer;
		ISymbolDrawingCache m_symbolDrawingCache;

		Device m_device;
		D3D10.Texture2D m_renderTexture;

		public TileControlD2D()
		{
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

		protected override void Render(System.Windows.Media.DrawingContext drawingContext, Size renderSize)
		{
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			m_interopImageSource.Lock();

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				InitTextureRenderSurface(renderWidth, renderHeight);

			if (m_tileRenderInvalid)
			{
				m_renderer.TileSizeChanged((int)this.TileSize); // XXX

				DoRender();
				m_device.Flush();
			}

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(renderSize));
		}

		public void SetRenderData(IRenderData renderData)
		{
			if (renderData is RenderData<RenderTileDetailed>)
				m_renderer = new RendererDetailed((RenderData<RenderTileDetailed>)renderData);
			else
				throw new NotSupportedException();

			InvalidateTileData();
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

			if (this.TileSize == 0)
			{
				m_renderTarget.EndDraw();
				return;
			}

			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;

			var m = Matrix3x2.Identity;
			m.M31 = (float)m_renderOffset.X;
			m.M32 = (float)m_renderOffset.Y;
			m_renderTarget.Transform = m;

			m_renderer.Render(m_renderTarget, m_gridSize.Width, m_gridSize.Height, (int)this.TileSize);

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
