using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;
using DXGI = SlimDX.DXGI;

namespace Dwarrowdelf.Client.TileControl
{
	public class WinFormsScene : IDisposable
	{
		IntPtr m_handle;
		Texture2D m_renderTarget;
		DXGI.SwapChain m_swapChain;
		Device m_device;
		SlimDX.Direct3D11.Buffer m_colorBuffer;

		SingleQuad11 m_scene;

		Texture2D m_tileTextureArray;
		ISymbolDrawingCache m_symbolDrawingCache;
		RenderData<RenderTileDetailed> m_map;

		int m_tileSize = 32;
		IntSize m_gridSize;

		public WinFormsScene(IntPtr handle)
		{
			m_handle = handle;

			m_device = Helpers11.CreateDevice();
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);
			m_scene = new SingleQuad11(m_device, m_colorBuffer);
		}

		public int TileSize
		{
			get { return m_tileSize; }
			set
			{
				m_tileSize = value;

				var columns = (int)Math.Ceiling((double)m_renderTarget.Description.Width / m_tileSize) | 1;
				var rows = (int)Math.Ceiling((double)m_renderTarget.Description.Height / m_tileSize) | 1;

				m_gridSize = new IntSize(columns, rows);
				m_map.Size = m_gridSize;
			}
		}

		public void Resize(int width, int height)
		{
			if (m_renderTarget != null) { m_renderTarget.Dispose(); m_renderTarget = null; }
			if (m_swapChain != null) { m_swapChain.Dispose(); m_swapChain = null; }

			Helpers11.CreateHwndRenderSurface(m_handle, m_device, width, height, out m_renderTarget, out m_swapChain);
			m_scene.SetRenderTarget(m_renderTarget);

			var columns = (int)Math.Ceiling((double)width / m_tileSize) | 1;
			var rows = (int)Math.Ceiling((double)height / m_tileSize) | 1;

			m_gridSize = new IntSize(columns, rows);
			m_map.Size = m_gridSize;
		}

		public void Render()
		{
			m_scene.SendMapData(m_map, m_gridSize.Width, m_gridSize.Height);
			m_scene.Render(m_tileSize, new System.Windows.Point(0, 0));
		}

		public void Present()
		{
			m_swapChain.Present(0, SlimDX.DXGI.PresentFlags.None);
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
					//InvalidateRender();
				}
			}
		}

		public void SetRenderData(IRenderData renderData)
		{
			if (!(renderData is RenderData<RenderTileDetailed>))
				throw new NotSupportedException();

			m_map = (RenderData<RenderTileDetailed>)renderData;
		}

		#region IDisposable
		bool m_disposed;

		~WinFormsScene()
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

				if (m_scene != null) { m_scene.Dispose(); m_scene = null; }
				if (m_renderTarget != null) { m_renderTarget.Dispose(); m_renderTarget = null; }
				if (m_swapChain != null) { m_swapChain.Dispose(); m_swapChain = null; }
				if (m_colorBuffer != null) { m_colorBuffer.Dispose(); m_colorBuffer = null; }
				if (m_tileTextureArray != null) { m_tileTextureArray.Dispose(); m_tileTextureArray = null; }
				if (m_device != null) { m_device.Dispose(); m_device = null; }

				m_disposed = true;
			}
		}
		#endregion
	}
}
