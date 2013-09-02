using System;
using System.IO;
using System.Windows;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class SceneHostWPF : ISceneHost, IDisposable
	{
		D3DImage11 m_interopImageSource;
		SharpDX.DXGI.Factory m_factory;

		Device ISceneHost.Device { get { return m_device; } }
		Device m_device;
		Texture2D m_renderTexture;
		RenderTargetView m_renderTargetView;

		IScene m_scene;

		IntSize2 m_renderSize;

		MyTraceSource trace = new MyTraceSource("Client.Render", "TileControl");

		public SceneHostWPF()
		{
			m_interopImageSource = new D3DImage11();
			m_interopImageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

			m_factory = new SharpDX.DXGI.Factory();

			using (var adapter = m_factory.GetAdapter(0))
				m_device = new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_10_0);
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

		void InitTextureRenderSurface(IntSize2 renderSize)
		{
			m_interopImageSource.SetBackBufferDX11(null);
			DH.Dispose(ref m_renderTargetView);
			DH.Dispose(ref m_renderTexture);

			if (renderSize.Width == 0 || renderSize.Height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}", renderSize);

			var texDesc = new Texture2DDescription()
			{
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				Width = renderSize.Width,
				Height = renderSize.Height,
				MipLevels = 1,
				SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				OptionFlags = ResourceOptionFlags.Shared,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			};

			m_renderTexture = new Texture2D(m_device, texDesc);
			m_renderTargetView = new RenderTargetView(m_device, m_renderTexture);

			m_interopImageSource.SetBackBufferDX11(m_renderTexture);
		}

		public IScene Scene
		{
			get { return m_scene; }
			set
			{
				if (m_scene == value)
					return;

				if (m_scene != null)
					m_scene.Detach();

				m_scene = value;

				if (m_scene != null)
				{
					m_scene.Attach(this);

					if (m_renderTexture != null)
						m_scene.OnRenderSizeChanged(m_renderSize);
				}
			}
		}

		public void SetRenderSize(IntSize2 renderSize)
		{
			if (renderSize == m_renderSize)
				return;

			InitTextureRenderSurface(renderSize);

			if (this.Scene != null)
				this.Scene.OnRenderSizeChanged(renderSize);

			var context = m_device.ImmediateContext;

			context.OutputMerger.SetTargets(m_renderTargetView);
			context.Rasterizer.SetViewport(new ViewportF(0, 0, renderSize.Width, renderSize.Height, 0.0f, 1.0f));

			m_renderSize = renderSize;
		}

		public void Render(System.Windows.Media.DrawingContext drawingContext)
		{
			if (m_disposed)
				return;

			if (this.Scene == null)
				return;

			var context = m_device.ImmediateContext;

			context.ClearRenderTargetView(m_renderTargetView, Color.DarkGoldenrod);

			this.Scene.Update(TimeSpan.FromSeconds(1)); // XXX this.RenderTimer.Elapsed);

			m_interopImageSource.Lock();

			this.Scene.Render();

			context.Flush();

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(0, 0, m_renderSize.Width, m_renderSize.Height));
		}

		#region IDisposable
		bool m_disposed;

		~SceneHostWPF()
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

				// Detach the scene
				this.Scene = null;
			}

			// Dispose unmanaged resources

			DH.Dispose(ref m_interopImageSource);
			DH.Dispose(ref m_renderTargetView);
			DH.Dispose(ref m_renderTexture);
			DH.Dispose(ref m_device);
			DH.Dispose(ref m_factory);

			m_disposed = true;
		}
		#endregion
	}
}
