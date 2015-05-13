//#define TESTCUBERENDERER
//#define OUTLINERENDERER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;
using System.Diagnostics;
using SharpDX.Toolkit.Graphics;
using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace Dwarrowdelf.Client
{
	sealed class MyGame : GameCore
	{
		readonly Camera m_camera;

		readonly KeyboardHandler m_keyboardHandler;
		readonly ViewGridProvider m_viewGridProvider;
		public MousePositionService MousePositionService { get; private set; }
		public SelectionService SelectionService { get; private set; }

		readonly TerrainRenderer m_terrainRenderer;
		readonly SymbolRenderer m_symbolRenderer;
		readonly SelectionRenderer m_selectionRenderer;
		readonly FPSCounter m_fpsCounter;

		readonly DebugAxesRenderer m_debugAxesRenderer;
#if TESTCUBERENDERER
		readonly TestCubeRenderer m_testCubeRenderer;
#endif

#if OUTLINERENDERER
		readonly ChunkOutlineRenderer m_outlineRenderer;
#endif
		public TerrainRenderer TerrainRenderer { get { return m_terrainRenderer; } }
		public RasterizerState RasterizerState { get; set; }
		public SelectionRenderer SelectionRenderer { get { return m_selectionRenderer; } }

		public ViewGridProvider ViewGrid { get { return m_viewGridProvider; } }
		public Camera Camera { get { return m_camera; } }

		public MyGame(SharpDXHost mainHost)
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			m_camera = new Camera();
			m_camera.LookAt(new Vector3(4, 0, 0), new Vector3(0, 0, 0), Vector3.UnitZ);

			m_viewGridProvider = new ViewGridProvider(this);

			m_keyboardHandler = new KeyboardHandler(this, mainHost, m_camera, m_viewGridProvider);
			base.Updatables.Add(m_keyboardHandler);

			m_fpsCounter = new FPSCounter(this);
			base.Updatables.Add(m_fpsCounter);


			var mainView = new GameSurfaceView(this.GraphicsDevice)
			{
				Camera = m_camera,
			};

			var axesView = new GameSurfaceView(this.GraphicsDevice)
			{
				Camera = m_camera,
			};

			var surface = ToDispose(new GameSurface(this.GraphicsDevice, mainHost));

			surface.Views.Add(mainView);
			surface.Views.Add(axesView);

			surface.SizeChanged += (w, h) =>
			{
				var vp = new ViewportF(0, 0, w, h);
				mainView.ViewPort = vp;
				mainView.Camera.SetAspect(vp.AspectRatio);

				axesView.ViewPort = new ViewportF(0, 0, 50, 50);
			};

			base.Surfaces.Add(surface);

			this.MousePositionService = new MousePositionService(this, mainHost, mainView);
			base.Updatables.Add(this.MousePositionService);

			this.SelectionService = new Client.SelectionService(this, mainHost, m_camera);

			m_terrainRenderer = ToDispose(new TerrainRenderer(this, m_camera, m_viewGridProvider));
			base.Updatables.Add(m_terrainRenderer);
			mainView.Drawables.Add(m_terrainRenderer);

			m_symbolRenderer = ToDispose(new SymbolRenderer(this, m_viewGridProvider));
			base.Updatables.Add(m_symbolRenderer);
			mainView.Drawables.Add(m_symbolRenderer);

			m_selectionRenderer = ToDispose(new SelectionRenderer(this));
			base.Updatables.Add(m_selectionRenderer);
			mainView.Drawables.Add(m_selectionRenderer);

			m_debugAxesRenderer = ToDispose(new DebugAxesRenderer(this));
			base.Updatables.Add(m_debugAxesRenderer);
			axesView.Drawables.Add(m_debugAxesRenderer);

#if OUTLINERENDERER
			m_outlineRenderer = ToDispose(new ChunkOutlineRenderer(this, m_terrainRenderer.ChunkManager));
			base.Updatables.Add(m_outlineRenderer);
			mainView.Drawables.Add(m_outlineRenderer);
#endif

#if TESTCUBERENDERER
			m_testCubeRenderer = ToDispose(new TestCubeRenderer(this));
			base.Updatables.Add(m_testCubeRenderer);
			mainView.Drawables.Add(m_testCubeRenderer);
#endif

			this.RasterizerState = this.GraphicsDevice.RasterizerStates.CullBack;
		}

		EnvironmentObject m_env;
		public EnvironmentObject Environment
		{
			get { return m_env; }
			set
			{
				var old = m_env;

				m_env = value;

				if (value != null)
				{
					var pos = value.Size.ToIntVector3().ToVector3();
					pos.X /= 2;
					pos.Y /= 2;
					pos.Z += 10;

					var target = new Vector3(0, 0, 0);

					//pos = new Vector3(-5, -4, 32); target = new Vector3(40, 40, 0);

					m_camera.LookAt(pos, target, Vector3.UnitZ);
				}

				//m_terrainRenderer.Enabled = map != null;
				if (m_selectionRenderer != null)
					m_selectionRenderer.IsEnabled = value != null;

				if (this.EnvironmentChanged != null)
					this.EnvironmentChanged(old, m_env);
			}
		}

		public event Action<EnvironmentObject, EnvironmentObject> EnvironmentChanged;

		public void GoTo(IntVector3 p)
		{
			// TODO: adjust view grid?

			var eye = p + new IntVector3(-1, 8, 30);

			m_camera.LookAt(eye.ToVector3(), p.ToVector3(), Vector3.UnitZ);
		}
	}
}
