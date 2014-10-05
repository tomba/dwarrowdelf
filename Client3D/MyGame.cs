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
		readonly CameraProvider m_cameraProvider;
		readonly KeyboardHandler m_keyboardHandler;
		readonly ViewGridProvider m_viewGridProvider;
		readonly TerrainRenderer m_terrainRenderer;
		readonly SymbolRenderer m_symbolRenderer;
		readonly SelectionRenderer m_selectionRenderer;

		readonly SceneRenderer m_sceneRenderer;
		readonly TestRenderer m_testRenderer;

		int m_frameCount;
		readonly Stopwatch m_fpsClock;

		public GraphicsDeviceManager GraphicsDeviceManager { get { return m_graphicsDeviceManager; } }

		public RasterizerState RasterizerState { get; set; }

		MovableManager m_movableManager = new MovableManager();

		public MyGame()
		{
			GlobalData.VoxelMap = CreateVoxelMap();

			this.IsMouseVisible = true;
			m_graphicsDeviceManager = new GraphicsDeviceManager(this);
			//this.GameSystems.Add(new EffectCompilerSystem(this));		// allows changing shaders runtime
			m_cameraProvider = new CameraProvider(this);
			m_keyboardHandler = new KeyboardHandler(this);

			m_viewGridProvider = new ViewGridProvider(this);
			m_terrainRenderer = new TerrainRenderer(this);
			m_symbolRenderer = new SymbolRenderer(this, m_movableManager);
			m_selectionRenderer = new SelectionRenderer(this);

			//m_sceneRenderer = new SceneRenderer(this);
			//m_testRenderer = new TestRenderer(this);

			Content.RootDirectory = "Content";

			m_fpsClock = new Stopwatch();

			AddMovables();

			var pos = GlobalData.VoxelMap.Size.ToIntVector3().ToVector3();
			pos.X /= 2;
			pos.Y /= 2;
			pos.Z += 10;

			var target = new Vector3(0, 0, 0);

			//pos = new Vector3(-5, -4, 32); target = new Vector3(40, 40, 0);

			m_cameraProvider.LookAt(pos, target, Vector3.UnitZ);
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

			var debugForm = new DebugForm(this, m_terrainRenderer);
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

			UpdateMovables(gameTime);

			base.Update(gameTime);

			if (this.GraphicsDevice.Viewport.Width != 0)
			{
				IntVector3 p;
				Direction d;

				if (MousePickVoxel(out p, out d))
				{
					m_selectionRenderer.Position = p;
					m_selectionRenderer.Direction = d;
					m_selectionRenderer.CursorEnabled = true;
				}
				else
				{
					m_selectionRenderer.CursorEnabled = false;
				}
			}
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
		public bool MousePickVoxel(out IntVector3 pos, out Direction face)
		{
			var form = (System.Windows.Forms.Form)this.Window.NativeWindow;
			var mousePos = form.PointToClient(System.Windows.Forms.Control.MousePosition);

			return MousePickVoxel(new IntVector2(mousePos.X, mousePos.Y), out pos, out face);
		}

		public bool MousePickVoxel(IntVector2 mousePos, out IntVector3 pos, out Direction face)
		{
			var camera = this.Services.GetService<CameraProvider>();

			var ray = Ray.GetPickRay(mousePos.X, mousePos.Y, this.GraphicsDevice.Viewport, camera.View * camera.Projection);

			IntVector3 outpos = new IntVector3();
			Direction outdir = Direction.None;

			var viewGrid = this.Services.GetService<ViewGridProvider>().ViewGrid;

			VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
				(x, y, z, dir) =>
				{
					var p = new IntVector3(x, y, z);

					if (viewGrid.Contains(p) == false)
						return false;

					var vx = GlobalData.VoxelMap.GetVoxel(p);

					// XXX IsEmpty would match for voxels with tree flag
					//if (vx.IsEmpty)
					if (vx.Type == VoxelType.Empty)
						return false;

					outpos = p;
					outdir = dir;

					return true;
				});

			pos = outpos;
			face = outdir;
			return face != Direction.None;
		}

	}
}
