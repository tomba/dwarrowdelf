using System;
using System.Windows;

using SlimDX.Direct3D11;

namespace Dwarrowdelf.Client.TileControl
{
	public class RendererD3D : IRenderer
	{
		D3DImageSlimDX m_interopImageSource;
		SingleQuad11 m_scene;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		SlimDX.Direct3D11.Buffer m_colorBuffer;

		ISymbolDrawingCache m_symbolDrawingCache;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		public RendererD3D()
		{
			m_interopImageSource = new D3DImageSlimDX();
			m_interopImageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

			m_device = Helpers11.CreateDevice();
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);
			m_scene = new SingleQuad11(m_device, m_colorBuffer);
		}

		void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// This fires when the screensaver kicks in, the machine goes into sleep or hibernate
			// and any other catastrophic losses of the d3d device from WPF's point of view

			if (m_interopImageSource.IsFrontBufferAvailable)
			{
				trace.TraceInformation("Frontbuffer available");

				m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
				m_interopImageSource.InvalidateD3DImage();
			}
			else
			{
				trace.TraceInformation("Frontbuffer not available");
			}
		}



		void InitTextureRenderSurface(int width, int height)
		{
			if (m_renderTexture != null)
			{
				m_interopImageSource.SetBackBufferSlimDX(null);
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			if (width == 0 || height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);
			m_renderTexture = Helpers11.CreateTextureRenderSurface(m_device, width, height);
			m_scene.SetRenderTarget(m_renderTexture);
			m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
		}




		public void Render(System.Windows.Media.DrawingContext drawingContext, Size renderSize, RenderContext ctx)
		{
			if (m_disposed)
				return;

			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			m_interopImageSource.Lock();

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				InitTextureRenderSurface(renderWidth, renderHeight);

			if (ctx.TileDataInvalid)
				m_scene.SendMapData(this.RenderData, ctx.RenderGridSize.Width, ctx.RenderGridSize.Height);

			if (ctx.TileRenderInvalid)
			{
				m_scene.Render((float)ctx.TileSize, ctx.RenderOffset);
				m_device.ImmediateContext.Flush();
			}

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(renderSize));
		}

		public RenderData<RenderTileDetailed> RenderData { get; set; }

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				if (m_symbolDrawingCache != null)
					m_symbolDrawingCache.DrawingsChanged -= OnDrawingsChanged;

				m_symbolDrawingCache = value;

				if (m_symbolDrawingCache != null)
					m_symbolDrawingCache.DrawingsChanged += OnDrawingsChanged;

				OnDrawingsChanged();
			}
		}

		void OnDrawingsChanged()
		{
			if (m_tileTextureArray != null)
			{
				m_tileTextureArray.Dispose();
				m_tileTextureArray = null;
			}

			m_tileTextureArray = Helpers11.CreateTextures11(m_device, m_symbolDrawingCache);

			if (m_scene != null)
				m_scene.SetTileTextures(m_tileTextureArray);
		}

		#region IDisposable
		bool m_disposed;

		~RendererD3D()
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

				if (m_scene != null)
				{
					m_scene.Dispose();
					m_scene = null;
				}

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

				if (m_tileTextureArray != null)
				{
					m_tileTextureArray.Dispose();
					m_tileTextureArray = null;
				}

				if (m_colorBuffer != null)
				{
					m_colorBuffer.Dispose();
					m_colorBuffer = null;
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
