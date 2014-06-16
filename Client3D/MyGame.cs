using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using System.Diagnostics;

namespace Client3D
{
	sealed class MyGame : Game
	{
		readonly GraphicsDeviceManager m_graphicsDeviceManager;
		readonly SceneRenderer m_sceneRenderer;
		readonly CameraProvider m_cameraProvider;
		readonly KeyboardManager m_keyboardManager;
		readonly TestRenderer m_testRenderer;

		KeyboardState m_keyboardState;

		int m_frameCount;
		readonly Stopwatch m_fpsClock;

		public MyGame()
		{
			this.IsMouseVisible = true;

			m_graphicsDeviceManager = new GraphicsDeviceManager(this);
			this.GameSystems.Add(new EffectCompilerSystem(this));		// allows changing shaders runtime
			m_testRenderer = new TestRenderer(this);
			m_sceneRenderer = new SceneRenderer(this);
			m_cameraProvider = new CameraProvider(this);
			m_keyboardManager = new KeyboardManager(this);

			Content.RootDirectory = "Content";

			m_fpsClock = new Stopwatch();
		}

		protected override void OnWindowCreated()
		{
			base.OnWindowCreated();

			var form = (System.Windows.Forms.Form)this.Window.NativeWindow;
			form.Width = 1024;
			form.Height = 800;
			form.Location = new System.Drawing.Point(300, 0);
			form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;

			var debugForm = new DebugForm();
			debugForm.Owner = (System.Windows.Forms.Form)this.Window.NativeWindow;
			debugForm.SetScene(m_testRenderer);
			debugForm.Show();
		}

		protected override void Initialize()
		{
			base.Initialize();

			m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.Window.ClientSizeChanged += (s, e) =>
					m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

		}

		protected override void BeginRun()
		{
			base.BeginRun();

			m_fpsClock.Start();
		}

		protected override void EndRun()
		{
			m_fpsClock.Stop();

			base.EndRun();
		}

		protected override void Update(GameTime gameTime)
		{
			m_frameCount++;
			if (m_fpsClock.ElapsedMilliseconds > 1000.0f)
			{
				var fpsText = string.Format("{0:F2} FPS", (float)m_frameCount * 1000 / m_fpsClock.ElapsedMilliseconds);
				m_frameCount = 0;
				m_fpsClock.Restart();

				this.Window.Title = fpsText;
			}

			m_keyboardState = m_keyboardManager.GetState();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			const float walkSpeek = 20f;
			const float rotSpeed = MathUtil.PiOverTwo;
			float dTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

			this.GraphicsDevice.Clear(Color.CornflowerBlue);

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
