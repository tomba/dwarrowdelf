using System;
using System.Windows;

using SlimDX.Direct3D11;

namespace Dwarrowdelf.Client.TileControl
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD3D : TileControlBase, IDisposable
	{
		D3DImageSlimDX m_interopImageSource;
		SingleQuad11 m_scene;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		SlimDX.Direct3D11.Buffer m_colorBuffer;

		RenderData<RenderTileDetailed> m_map;
		ISymbolDrawingCache m_symbolDrawingCache;

		public TileControlD3D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D3DImageSlimDX();
			m_interopImageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

			this.Loaded += new RoutedEventHandler(OnLoaded);

			m_device = Helpers11.CreateDevice();
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);
			m_scene = new SingleQuad11(m_device, m_colorBuffer);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			trace.TraceInformation("OnLoaded");

			InvalidateVisual();
		}


		void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// This fires when the screensaver kicks in, the machine goes into sleep or hibernate
			// and any other catastrophic losses of the d3d device from WPF's point of view

			if (m_interopImageSource.IsFrontBufferAvailable)
			{
				trace.TraceInformation("Frontbuffer available");

				var renderWidth = (int)Math.Ceiling(this.RenderSize.Width);
				var renderHeight = (int)Math.Ceiling(this.RenderSize.Height);

				m_interopImageSource.Lock();

				InitTextureRenderSurface(renderWidth, renderHeight);

				m_interopImageSource.Unlock();

				InvalidateTileRender();
			}
			else
			{
				trace.TraceInformation("Frontbuffer not available");

				if (m_renderTexture != null)
				{
					m_interopImageSource.SetBackBufferSlimDX(null);
					m_renderTexture.Dispose();
					m_renderTexture = null;
				}
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

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);
			m_renderTexture = Helpers11.CreateTextureRenderSurface(m_device, width, height);
			m_scene.SetRenderTarget(m_renderTexture);
			m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
		}



		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceVerbose("ArrangeOverride({0})", arrangeBounds);

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

			if (m_tileDataInvalid)
				m_scene.SendMapData(m_map, m_gridSize.Width, m_gridSize.Height);

			if (m_tileRenderInvalid)
			{
				m_scene.Render((float)this.TileSize, m_renderOffset);
				m_device.ImmediateContext.Flush();
			}

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(renderSize));
		}





		public void SetRenderData(IRenderData renderData)
		{
			if (!(renderData is RenderData<RenderTileDetailed>))
				throw new NotSupportedException();

			m_map = (RenderData<RenderTileDetailed>)renderData;

			InvalidateTileData();
		}

		public void InvalidateSymbols()
		{
			if (m_tileTextureArray != null)
			{
				m_tileTextureArray.Dispose();
				m_tileTextureArray = null;
			}

			m_tileTextureArray = Helpers11.CreateTextures11(m_device, m_symbolDrawingCache);

			if (m_scene != null)
			{
				m_scene.SetTileTextures(m_tileTextureArray);
				InvalidateTileRender();
			}
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;

				if (m_tileTextureArray != null)
				{
					m_tileTextureArray.Dispose();
					m_tileTextureArray = null;
				}

				m_tileTextureArray = Helpers11.CreateTextures11(m_device, m_symbolDrawingCache);

				if (m_scene != null)
				{
					m_scene.SetTileTextures(m_tileTextureArray);
					InvalidateTileRender();
				}
			}
		}


		#region IDisposable
		bool m_disposed;

		~TileControlD3D()
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
