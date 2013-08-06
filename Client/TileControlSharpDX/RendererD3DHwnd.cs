using System;
using System.IO;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererD3DSharpDXHwnd : Component, ISceneHost
	{
		SharpDX.DXGI.Factory m_factory;
		IntPtr m_windowHandle;
		SharpDX.DXGI.SwapChain m_swapChain;

		Device ISceneHost.Device { get { return m_device; } }
		Device m_device;
		Texture2D m_renderTexture;
		RenderTargetView m_renderTargetView;

		IntSize2 m_renderSize;

		IScene m_scene;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		public RendererD3DSharpDXHwnd(IntPtr windowHandle)
		{
			m_windowHandle = windowHandle;

			m_factory = ToDispose(new SharpDX.DXGI.Factory());

			using (var adapter = m_factory.GetAdapter(0))
				m_device = ToDispose(new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_10_0));
		}

		void InitTextureRenderSurface(IntSize2 renderSize)
		{
			if (renderSize.Width == 0 || renderSize.Height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}", renderSize);

			var swapChainDesc = new SwapChainDescription()
			{
				BufferCount = 1,
				ModeDescription = new ModeDescription(renderSize.Width, renderSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
				IsWindowed = true,
				OutputHandle = m_windowHandle,
				SampleDescription = new SampleDescription(1, 0),
				SwapEffect = SwapEffect.Discard,
				Usage = Usage.RenderTargetOutput,
			};

			RemoveAndDispose(ref m_renderTexture);
			RemoveAndDispose(ref m_swapChain);

			m_swapChain = ToDispose(new SwapChain(m_factory, m_device, swapChainDesc));
			m_renderTexture = ToDispose(Texture2D.FromSwapChain<Texture2D>(m_swapChain, 0));
			m_renderTargetView = new RenderTargetView(m_device, m_renderTexture);
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
					{
						m_scene.OnRenderSizeChanged(m_renderSize);
					}
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
			context.Rasterizer.SetViewports(new Viewport(0, 0, renderSize.Width, renderSize.Height, 0.0f, 1.0f));

			m_renderSize = renderSize;
		}

		public void Render()
		{
			if (this.IsDisposed)
				return;

			if (this.Scene == null)
				return;

			var context = m_device.ImmediateContext;

			context.ClearRenderTargetView(m_renderTargetView, Color.DarkGoldenrod);

			this.Scene.Update(TimeSpan.FromSeconds(1)); // XXX this.RenderTimer.Elapsed);
			m_scene.Render();

			m_swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
		}
	}
}
