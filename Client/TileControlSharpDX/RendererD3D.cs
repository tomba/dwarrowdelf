using System;
using System.Windows;

using SharpDX.Direct3D11;
using System.IO;
using SharpDX;
using SharpDX.Direct3D;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererD3DSharpDX : IDisposable
	{
		D3DImageSharpDX m_interopImageSource;
		SingleQuad11 m_scene;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		RenderData<RenderTile> m_renderData;
		SharpDX.DXGI.Factory m_factory;

		public RendererD3DSharpDX(RenderData<RenderTile> renderData)
		{
			m_renderData = renderData;

			m_interopImageSource = new D3DImageSharpDX();
			m_interopImageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

			m_factory = new SharpDX.DXGI.Factory();

			using (var adapter = m_factory.GetAdapter(0))
				m_device = new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_10_0);

			m_scene = new SingleQuad11(m_device);
		}

		void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// This fires when the screensaver kicks in, the machine goes into sleep or hibernate
			// and any other catastrophic losses of the d3d device from WPF's point of view

			if (m_interopImageSource.IsFrontBufferAvailable)
			{
				trace.TraceInformation("Frontbuffer available");

				m_interopImageSource.SetBackBufferDX11(m_renderTexture);
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
				m_interopImageSource.SetBackBufferDX11(null);
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			if (width == 0 || height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);

			var texDesc = new Texture2DDescription()
			{
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				Width = width,
				Height = height,
				MipLevels = 1,
				SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				OptionFlags = ResourceOptionFlags.Shared,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			};

			m_renderTexture = new Texture2D(m_device, texDesc);

			m_scene.SetRenderTarget(m_renderTexture);
			m_interopImageSource.SetBackBufferDX11(m_renderTexture);
		}


		public void Render(System.Windows.Media.DrawingContext drawingContext, Size renderSize, IntSize2 gridSize,
			float tileSize, Point renderOffset, bool tileDataInvalid)
		{
			if (m_disposed)
				return;

			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			m_interopImageSource.Lock();

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
			{
				InitTextureRenderSurface(renderWidth, renderHeight);
				// force a full update if we re-allocate the render surface
				tileDataInvalid = true;
			}

			if (tileDataInvalid)
			{
				if (m_renderData.Size != gridSize)
					throw new Exception();

				m_scene.SendMapData(m_renderData.Grid, gridSize.Width, gridSize.Height);
			}

			m_scene.Render(tileSize, new Vector2((float)renderOffset.X, (float)renderOffset.Y));
			m_device.ImmediateContext.Flush();

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(renderSize));
		}

		public void SetTileSet(ITileSet tileSet)
		{
			if (m_tileTextureArray != null)
			{
				m_tileTextureArray.Dispose();
				m_tileTextureArray = null;
			}

			m_tileTextureArray = Helpers11.CreateTextures11(m_device, tileSet);

			if (m_scene != null)
				m_scene.SetTileTextures(m_tileTextureArray);
		}

		#region IDisposable
		bool m_disposed;

		~RendererD3DSharpDX()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				// Dispose managed resources.
			}

			// Dispose unmanaged resources

			DH.Dispose(ref m_scene);
			DH.Dispose(ref m_interopImageSource);
			DH.Dispose(ref m_renderTexture);
			DH.Dispose(ref m_tileTextureArray);
			DH.Dispose(ref m_device);
			DH.Dispose(ref m_factory);

			m_disposed = true;
		}
		#endregion
	}
}
