using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;

namespace Dwarrowdelf.Client
{
	class GameSurface : Component
	{
		readonly GraphicsDevice m_device;
		readonly SharpDXHost m_host;

		GraphicsPresenter m_presenter;
		readonly PixelFormat m_pixelFormat = PixelFormat.R8G8B8A8.UNorm;

		public event Action<int, int> SizeChanged;

		public List<GameSurfaceView> Views { get; private set; }

		public GameSurface(GraphicsDevice device, SharpDXHost host)
		{
			m_device = device;
			m_host = host;

			this.Views = new List<GameSurfaceView>();
		}

		public void Draw()
		{
			SetupPresenter();

			m_device.Presenter = m_presenter;
			m_device.SetRenderTargets(m_presenter.DepthStencilBuffer, m_presenter.BackBuffer);

			m_device.Clear(SharpDX.Color.CornflowerBlue);

			foreach (var view in this.Views)
			{
				view.Draw();
			}

			m_device.Present();

			m_device.Presenter = null;
		}

		void SetupPresenter()
		{
			int width = m_host.HostedWindowWidth;
			int height = m_host.HostedWindowHeight;

			bool sizeChanged;

			if (m_presenter == null)
			{
				var presentParams = new PresentationParameters(width, height, m_host.Handle, m_pixelFormat)
				{
					PresentationInterval = PresentInterval.Immediate,
				};

				m_presenter = ToDispose(new SwapChainGraphicsPresenter(m_device, presentParams));

				sizeChanged = true;
			}
			else
			{
				// Resize is a no-op if the size hasn't changed
				sizeChanged = m_presenter.Resize(width, height, m_pixelFormat);
			}

			if (sizeChanged)
			{
				if (this.SizeChanged != null)
					this.SizeChanged(width, height);
			}
		}
	}
}
