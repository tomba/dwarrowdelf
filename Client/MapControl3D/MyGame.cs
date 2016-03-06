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
		public Camera Camera { get; private set; }

		public CameraKeyHandler CameraKeyHandler { get; private set; }
		public ViewGridProvider ViewGridProvider { get; private set; }
		public MousePositionService MousePositionService { get; private set; }
		public SelectionService SelectionService { get; private set; }
		public CursorService CursorService { get; private set; }
		public CameraMoveService CameraMoveService { get; private set; }
		public ViewGridAdjusterService ViewGridAdjusterService { get; private set; }

		public TerrainRenderer TerrainRenderer { get; private set; }
		readonly SymbolRenderer m_symbolRenderer;
		readonly SelectionRenderer m_selectionRenderer;
		readonly FPSCounter m_fpsCounter;
		readonly DesignationRenderer m_designationRenderer;

		readonly DebugAxesRenderer m_debugAxesRenderer;
#if TESTCUBERENDERER
		readonly TestCubeRenderer m_testCubeRenderer;
#endif

#if OUTLINERENDERER
		readonly ChunkOutlineRenderer m_outlineRenderer;
#endif
		public RasterizerState RasterizerState { get; set; }

		public MyGame(MapControl3D mainHost)
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			this.Camera = new Camera();
			this.Camera.LookAt(new Vector3(4, 0, 0), new Vector3(0, 0, 0), Vector3.UnitZ);

			this.CameraMoveService = new CameraMoveService(this.Camera);
			base.Updatables.Add(this.CameraMoveService);

			this.ViewGridAdjusterService = new ViewGridAdjusterService(this, mainHost);
			base.Updatables.Add(this.ViewGridAdjusterService);

			this.ViewGridProvider = new ViewGridProvider(this);

			this.CameraKeyHandler = new CameraKeyHandler(this, mainHost);
			base.Updatables.Add(this.CameraKeyHandler);

			m_fpsCounter = new FPSCounter(this);
			base.Updatables.Add(m_fpsCounter);

			this.CursorService = new CursorService(this, mainHost);


			var mainView = new GameSurfaceView(this.GraphicsDevice)
			{
				Camera = this.Camera,
			};

			var axesView = new GameSurfaceView(this.GraphicsDevice)
			{
				Camera = this.Camera,
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

			this.SelectionService = new Client.SelectionService(this, mainHost, this.Camera);

			this.TerrainRenderer = ToDispose(new TerrainRenderer(this, this.Camera, this.ViewGridProvider));
			base.Updatables.Add(this.TerrainRenderer);
			mainView.Drawables.Add(this.TerrainRenderer);

			m_symbolRenderer = ToDispose(new SymbolRenderer(this, this.ViewGridProvider));
			base.Updatables.Add(m_symbolRenderer);
			mainView.Drawables.Add(m_symbolRenderer);

			m_selectionRenderer = ToDispose(new SelectionRenderer(this));
			base.Updatables.Add(m_selectionRenderer);
			mainView.Drawables.Add(m_selectionRenderer);

			m_designationRenderer = ToDispose(new DesignationRenderer(this));
			base.Updatables.Add(m_designationRenderer);
			mainView.Drawables.Add(m_designationRenderer);

			m_debugAxesRenderer = ToDispose(new DebugAxesRenderer(this));
			base.Updatables.Add(m_debugAxesRenderer);
			axesView.Drawables.Add(m_debugAxesRenderer);

#if OUTLINERENDERER
			m_outlineRenderer = ToDispose(new ChunkOutlineRenderer(this, this.TerrainRenderer.ChunkManager));
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
				if (m_env == value)
					return;

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

					this.Camera.LookAt(pos, target, Vector3.UnitZ);
				}

				//m_terrainRenderer.Enabled = map != null;
				if (m_selectionRenderer != null)
					m_selectionRenderer.IsEnabled = value != null;

				if (this.EnvironmentChanged != null)
					this.EnvironmentChanged(old, m_env);
			}
		}

		public event Action<EnvironmentObject, EnvironmentObject> EnvironmentChanged;


		Vector3 GetEyeFromLookTarget(IntVector3 p)
		{
			var v = new Vector3(-1, 8, 32.5f) / 32.5f;

			return p.ToVector3() + v * (this.ViewGridAdjusterService.Height + 0.5f);
		}

		public void CameraLookAt(IntVector3 p)
		{
			var eye = GetEyeFromLookTarget(p);
			var target = p.ToVector3();

			this.Camera.LookAt(eye, target, Vector3.UnitZ);
		}

		public void CameraMoveTo(IntVector3 p)
		{
			var eye = GetEyeFromLookTarget(p);
			var target = p.ToVector3();

			this.CameraMoveService.Move(eye, target);
		}
	}
}
