using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.IO;
using Device = SharpDX.Direct3D11.Device;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererD3DSharpDXHwnd : Component
	{
		SingleQuad11 m_scene;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		RenderData<RenderTile> m_renderData;

		IntPtr m_windowHandle;
		SharpDX.DXGI.SwapChain m_swapChain;
		SharpDX.DXGI.Factory m_factory;

		public RendererD3DSharpDXHwnd(RenderData<RenderTile> renderData, IntPtr windowHandle)
		{
			m_renderData = renderData;
			m_windowHandle = windowHandle;

			m_factory = ToDispose(new SharpDX.DXGI.Factory());

			using (var adapter = m_factory.GetAdapter(0))
				m_device = ToDispose(new Device(adapter, DeviceCreationFlags.Debug, FeatureLevel.Level_10_0));

			m_scene = ToDispose(new SingleQuad11(m_device));
		}

		void InitTextureRenderSurface(int width, int height)
		{
			if (width == 0 || height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);

			var swapChainDesc = new SwapChainDescription()
			{
				BufferCount = 1,
				ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
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

			m_scene.SetRenderTarget(m_renderTexture);
		}

		public void Render(IntSize2 renderSize, IntSize2 gridSize, float tileSize, IntPoint2 renderOffset, bool tileDataInvalid)
		{
			if (this.IsDisposed)
				return;

			var renderWidth = renderSize.Width;
			var renderHeight = renderSize.Height;

			if (m_renderTexture == null || m_renderTexture.Description.Width != renderWidth || m_renderTexture.Description.Height != renderHeight)
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

			m_scene.Render(tileSize, new Vector2(renderOffset.X, renderOffset.Y));

			m_swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
		}

		public void SetTileSet(TileSet tileSet)
		{
			RemoveAndDispose(ref m_tileTextureArray);

			m_tileTextureArray = ToDispose(Helpers11.CreateTextures11(m_device, tileSet));

			if (m_scene != null)
				m_scene.SetTileTextures(m_tileTextureArray);
		}
	}
}
