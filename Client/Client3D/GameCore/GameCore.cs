using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;

namespace Dwarrowdelf.Client
{
	abstract class GameCore : Component
	{
		public GraphicsDevice GraphicsDevice { get; private set; }

		TimerTick m_timer;

		public readonly List<IGameUpdatable> Updatables = new List<IGameUpdatable>();
		public readonly List<GameSurface> Surfaces = new List<GameSurface>();

		public ContentPool Content { get; private set; }

		protected GameCore()
		{
			CreateDevice();

			this.Content = ToDispose(new ContentPool(this.GraphicsDevice));
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);

			// Dispose the static WICHelper
			WICHelper.Dispose();
		}

		void CreateDevice()
		{
			var deviceFlags = DeviceCreationFlags.None;
			deviceFlags |= DeviceCreationFlags.SingleThreaded;
#if DEBUG
			deviceFlags |= DeviceCreationFlags.Debug;
#endif
			this.GraphicsDevice = ToDispose(GraphicsDevice.New(DriverType.Hardware, deviceFlags));
		}

		public void Start()
		{
			m_timer = new TimerTick();

			System.Windows.Media.CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		public void Stop()
		{
			System.Windows.Media.CompositionTarget.Rendering -= CompositionTarget_Rendering;
		}


		TimeSpan m_lastRender;

		void CompositionTarget_Rendering(object sender, EventArgs _e)
		{
			var e = (System.Windows.Media.RenderingEventArgs)_e;

			var diff = e.RenderingTime - m_lastRender;

			if (diff == TimeSpan.Zero)
				return;

			m_lastRender = e.RenderingTime;

			m_timer.Tick();

			var time = m_timer.TotalTime;

			// UPDATE 
			foreach (var updatable in this.Updatables)
			{
				updatable.Update(time);
			}

			// DRAW
			foreach (var target in this.Surfaces)
			{
				target.Draw();
			}
		}
	}
}
