using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;

namespace Client3D
{
	sealed class MyGame : Game
	{
		readonly GraphicsDeviceManager m_graphicsDeviceManager;
		readonly SceneRenderer m_sceneRenderer;
		readonly CameraProvider m_cameraProvider;

		public MyGame()
		{
			m_graphicsDeviceManager = new GraphicsDeviceManager(this);
			m_sceneRenderer = new SceneRenderer(this);
			m_cameraProvider = new CameraProvider(this);

			Content.RootDirectory = "Content";
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			base.Draw(gameTime);
		}
	}
}
