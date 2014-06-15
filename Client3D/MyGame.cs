using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;

namespace Client3D
{
	sealed class MyGame : Game
	{
		readonly GraphicsDeviceManager m_graphicsDeviceManager;
		readonly SceneRenderer m_sceneRenderer;
		readonly CameraProvider m_cameraProvider;
		readonly KeyboardManager m_keyboardManager;

		KeyboardState m_keyboardState;

		public MyGame()
		{
			this.IsMouseVisible = true;

			m_graphicsDeviceManager = new GraphicsDeviceManager(this);
			m_sceneRenderer = new SceneRenderer(this);
			m_cameraProvider = new CameraProvider(this);
			m_keyboardManager = new KeyboardManager(this);

			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			base.Initialize();

			m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.Window.ClientSizeChanged += (s, e) =>
					m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);
		}

		protected override void Update(GameTime gameTime)
		{
			m_keyboardState = m_keyboardManager.GetState();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			const float walkSpeek = 20f;
			const float rotSpeed = MathUtil.PiOverTwo;
			float dTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

			GraphicsDevice.Clear(Color.CornflowerBlue);

			if (m_keyboardState.IsKeyDown(Keys.F4) && m_keyboardState.IsKeyDown(Keys.LeftAlt))
				this.Exit();

			if (m_keyboardState.IsKeyDown(Keys.W))
				m_cameraProvider.Walk(walkSpeek * dTime);
			else if (m_keyboardState.IsKeyDown(Keys.S))
				m_cameraProvider.Walk(-walkSpeek * dTime);

			if (m_keyboardState.IsKeyDown(Keys.D))
				m_cameraProvider.Strafe(walkSpeek * dTime);
			else if (m_keyboardState.IsKeyDown(Keys.A))
				m_cameraProvider.Strafe(-walkSpeek * dTime);

			if (m_keyboardState.IsKeyDown(Keys.E))
				m_cameraProvider.Climb(walkSpeek * dTime);
			else if (m_keyboardState.IsKeyDown(Keys.Q))
				m_cameraProvider.Climb(-walkSpeek * dTime);

			if (m_keyboardState.IsKeyDown(Keys.Up))
				m_cameraProvider.Pitch(-rotSpeed * dTime);
			else if (m_keyboardState.IsKeyDown(Keys.Down))
				m_cameraProvider.Pitch(rotSpeed * dTime);

			if (m_keyboardState.IsKeyDown(Keys.Left))
				m_cameraProvider.RotateZ(-rotSpeed * dTime);
			else if (m_keyboardState.IsKeyDown(Keys.Right))
				m_cameraProvider.RotateZ(rotSpeed * dTime);

			base.Draw(gameTime);
		}
	}
}
