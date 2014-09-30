using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using System.Diagnostics;
using SharpDX.Toolkit.Graphics;
using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace Client3D
{
	sealed class MyGame : Game
	{
		readonly GraphicsDeviceManager m_graphicsDeviceManager;
		readonly SceneRenderer m_sceneRenderer;
		readonly CameraProvider m_cameraProvider;
		readonly KeyboardManager m_keyboardManager;
		readonly TerrainRenderer m_terrainRenderer;
		readonly TestRenderer m_testRenderer;
		readonly SymbolRenderer m_symbolRenderer;

		int m_frameCount;
		readonly Stopwatch m_fpsClock;

		public GraphicsDeviceManager GraphicsDeviceManager { get { return m_graphicsDeviceManager; } }
		public TerrainRenderer TerrainRenderer { get { return m_terrainRenderer; } }

		public RasterizerState RasterizerState { get; set; }

		MovableManager m_movableManager = new MovableManager();

		public MyGame()
		{
			GlobalData.VoxelMap = CreateVoxelMap();

			this.IsMouseVisible = true;
			m_graphicsDeviceManager = new GraphicsDeviceManager(this);
			//this.GameSystems.Add(new EffectCompilerSystem(this));		// allows changing shaders runtime
			m_keyboardManager = new KeyboardManager(this);
			m_cameraProvider = new CameraProvider(this);

			m_terrainRenderer = new TerrainRenderer(this);
			//m_sceneRenderer = new SceneRenderer(this);
			//m_testRenderer = new TestRenderer(this);
			m_symbolRenderer = new SymbolRenderer(this, m_movableManager);

			Content.RootDirectory = "Content";

			m_fpsClock = new Stopwatch();

			AddMovables();
		}

		void AddMovables()
		{
			MovableObject ob;

			Func<IntVector2, IntVector3> floor = v2 =>
			{
				var map = GlobalData.VoxelMap;
				for (int z = map.Depth - 1; z > 0; --z)
				{
					var v = new IntVector3(v2, z);
					if (map.GetVoxel(v).IsEmpty == false)
						return v.Up;
				}

				throw new Exception();
			};

			ob = new MovableObject(SymbolID.Player, GameColor.Green);
			m_movableManager.AddMovable(ob);
			ob.Move(floor(new IntVector2(10, 10)));

			ob = new MovableObject(SymbolID.Player, GameColor.Blue);
			m_movableManager.AddMovable(ob);
			ob.Move(floor(new IntVector2(5, 10)));
		}

		VoxelMap CreateVoxelMap()
		{
			const string mapname = "voxelmap.dat";

			bool newmap = false;

			VoxelMap map;

			if (newmap == false && System.IO.File.Exists(mapname))
			{
				var sw = Stopwatch.StartNew();
				map = VoxelMap.Load(mapname);
				sw.Stop();
				Trace.TraceInformation("load map {0} ms", sw.ElapsedMilliseconds);
			}
			else
			{
				var sw = Stopwatch.StartNew();

				//map = VoxelMap.CreateBallMap(32, 16);
				//map = VoxelMap.CreateCubeMap(32, 1);
				map = VoxelMapGen.CreateTerrain(new IntSize3(128, 128, 64));

				map.CheckVisibleFaces(true);

				sw.Stop();
				Trace.TraceInformation("create map {0} ms", sw.ElapsedMilliseconds);

				sw = Stopwatch.StartNew();
				map.Save(mapname);
				sw.Stop();
				Trace.TraceInformation("save map {0} ms", sw.ElapsedMilliseconds);
			}

			return map;
		}

		protected override void OnWindowCreated()
		{
			base.OnWindowCreated();

			var form = (System.Windows.Forms.Form)this.Window.NativeWindow;
			form.Width = 1024;
			form.Height = 800;
			form.Location = new System.Drawing.Point(300, 0);
			form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			form.MouseDown += (s, e) =>
			{
				if (m_terrainRenderer != null)
					m_terrainRenderer.ClickPos = new Dwarrowdelf.IntVector2(e.X, e.Y);
			};
			form.KeyPress += (s, e) =>
			{
				switch (e.KeyChar)
				{
					case '>':
						m_terrainRenderer.ViewCorner2 = m_terrainRenderer.ViewCorner2 + Direction.Down;
						break;
					case '<':
						m_terrainRenderer.ViewCorner2 = m_terrainRenderer.ViewCorner2 + Direction.Up;
						break;
					case '1':
						m_cameraProvider.LookAt(m_cameraProvider.Position,
							m_cameraProvider.Position + new Vector3(0, -1, -10),
							Vector3.UnitZ);
						break;
					case '2':
						m_cameraProvider.LookAt(m_cameraProvider.Position,
							m_cameraProvider.Position + new Vector3(1, 1, -1),
							Vector3.UnitZ);
						break;
				}
			};

			var debugForm = new DebugForm(this);
			debugForm.Owner = (System.Windows.Forms.Form)this.Window.NativeWindow;
			debugForm.Show();
		}

		protected override void Initialize()
		{
			base.Initialize();

			m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.Window.ClientSizeChanged += (s, e) =>
					m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.RasterizerState = this.GraphicsDevice.RasterizerStates.CullBack;
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

		void HandleKeyboard(KeyboardState keyboardState)
		{
			if (keyboardState.IsKeyDown(Keys.F4) && keyboardState.IsKeyDown(Keys.LeftAlt))
				this.Exit();

			switch (GlobalData.ControlMode)
			{
				case ControlMode.Fps:
					HandleFpsKeyboard(keyboardState);
					break;

				case ControlMode.Rts:
					HandleRtsKeyboard(keyboardState);
					break;

				default:
					throw new Exception();
			}

			if (keyboardState.IsKeyPressed(Keys.R))
			{
				var form = (System.Windows.Forms.Form)this.Window.NativeWindow;
				var p = form.PointToClient(System.Windows.Forms.Control.MousePosition);

				var camera = this.Services.GetService<ICameraService>();

				var ray = Ray.GetPickRay(p.X, p.Y, this.GraphicsDevice.Viewport, camera.View * camera.Projection);

				VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
					(x, y, z, vx, dir) =>
					{
						var l = new IntVector3(x, y, z);

						if (GlobalData.VoxelMap.Size.Contains(l) == false)
							return true;

						GlobalData.VoxelMap.SetVoxel(l, Voxel.Rock);

						return false;
					});
			}
		}

		void HandleFpsKeyboard(KeyboardState keyboardState)
		{
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;
			float dTime = (float)this.gameTime.ElapsedGameTime.TotalSeconds;
			float mul = 1f;

			if (keyboardState.IsKeyDown(Keys.Shift))
				mul = 0.2f;

			if (keyboardState.IsKeyDown(Keys.W))
				m_cameraProvider.Walk(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.S))
				m_cameraProvider.Walk(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.D))
				m_cameraProvider.Strafe(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.A))
				m_cameraProvider.Strafe(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.E))
				m_cameraProvider.Climb(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Q))
				m_cameraProvider.Climb(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Up))
				m_cameraProvider.Pitch(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Down))
				m_cameraProvider.Pitch(rotSpeed * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Left))
				m_cameraProvider.RotateZ(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Right))
				m_cameraProvider.RotateZ(rotSpeed * dTime * mul);
		}

		void HandleRtsKeyboard(KeyboardState keyboardState)
		{
			float dTime = (float)this.gameTime.ElapsedGameTime.TotalSeconds;
			float mul = 1f;
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;

			if (keyboardState.IsKeyDown(Keys.Shift))
				mul = 0.2f;

			Vector3 v = new Vector3();

			if (keyboardState.IsKeyDown(Keys.E))
				v.Z = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.Q))
				v.Z = -walkSpeek * dTime * mul;

			if (keyboardState.IsKeyDown(Keys.W))
				v.Y = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.S))
				v.Y = -walkSpeek * dTime * mul;

			if (keyboardState.IsKeyDown(Keys.D))
				v.X = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.A))
				v.X = -walkSpeek * dTime * mul;

			if (!v.IsZero)
			{
				m_cameraProvider.Move(v);

				if (GlobalData.AlignViewGridToCamera && v.Z != 0)
				{
					var c = this.TerrainRenderer.ViewCorner2;
					c.Z = (int)m_cameraProvider.Position.Z - 32;
					this.TerrainRenderer.ViewCorner2 = c;
				}
			}

			if (keyboardState.IsKeyDown(Keys.Up))
				m_cameraProvider.Pitch(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Down))
				m_cameraProvider.Pitch(rotSpeed * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Left))
				m_cameraProvider.RotateZ(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Right))
				m_cameraProvider.RotateZ(rotSpeed * dTime * mul);
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

			var keyboardState = m_keyboardManager.GetState();

			HandleKeyboard(keyboardState);

			UpdateMovables(gameTime);

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			this.GraphicsDevice.Clear(Color.CornflowerBlue);

			this.GraphicsDevice.SetRasterizerState(this.RasterizerState);

			base.Draw(gameTime);
		}

		void UpdateMovables(GameTime gameTime)
		{
			if (gameTime.FrameCount % 60 != 0)
				return;

			var map = GlobalData.VoxelMap;

			foreach (var ob in m_movableManager.Movables)
			{
				var p = ob.Position;

				foreach (var d in DirectionExtensions.CardinalDirections)
				{
					var n = p + d.Reverse();

					if (map.Size.Contains(n) == false)
						continue;

					if (GlobalData.VoxelMap.GetVoxel(n).IsEmpty)
					{
						ob.Move(n);
						break;
					}
				}
			}
		}
	}
}
