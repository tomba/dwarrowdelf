using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;

namespace Dwarrowdelf.Client
{
	class GameSurfaceView
	{
		public List<IGameDrawable> Drawables { get; private set; }
		public ViewportF ViewPort { get; set; }
		public Camera Camera { get; set; }

		readonly GraphicsDevice m_device;

		public GameSurfaceView(GraphicsDevice device)
		{
			m_device = device;

			this.Drawables = new List<IGameDrawable>();
		}

		public void Draw()
		{
			m_device.SetViewport(this.ViewPort);

			foreach (var drawable in this.Drawables)
			{
				drawable.Draw(this.Camera);
			}
		}
	}
}
