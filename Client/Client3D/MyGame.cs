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
		readonly TerrainRenderer m_terrainRenderer;
		readonly SymbolRenderer m_symbolRenderer;
		readonly SelectionRenderer m_selectionRenderer;
		readonly FPSCounter m_fpsCounter;

		readonly TestCubeRenderer m_testCubeRenderer;
		readonly DebugAxesRenderer m_debugAxesRenderer;
		readonly ChunkOutlineRenderer m_outlineRenderer;

		public TerrainRenderer TerrainRenderer { get { return m_terrainRenderer; } }
		public RasterizerState RasterizerState { get; set; }

		public ViewGridProvider ViewGrid { get { return m_viewGridProvider; } }

		public MyGame(SharpDXHost mainHost)
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			m_camera = new Camera();
			m_camera.LookAt(new Vector3(4, 0, 0), new Vector3(0, 0, 0), Vector3.UnitZ);

			m_keyboardHandler = new KeyboardHandler(mainHost, m_camera, m_viewGridProvider);
			//m_mouseManager = new MouseManager(this);

			m_viewGridProvider = new ViewGridProvider();

			m_terrainRenderer = new TerrainRenderer(this.GraphicsDevice, m_camera, m_viewGridProvider);
			//m_symbolRenderer = new SymbolRenderer(this, m_movableManager);
			m_selectionRenderer = new SelectionRenderer(this.GraphicsDevice, m_camera, m_viewGridProvider, mainHost);
			m_debugAxesRenderer = new DebugAxesRenderer(this.GraphicsDevice);

			//m_outlineRenderer = new ChunkOutlineRenderer(this.GraphicsDevice, m_terrainRenderer.ChunkManager);
			//base.Updatables.Add(m_outlineRenderer);

			base.Updatables.Add(m_keyboardHandler);
			base.Updatables.Add(m_terrainRenderer);
			base.Updatables.Add(m_debugAxesRenderer);
			base.Updatables.Add(m_selectionRenderer);

			
			m_fpsCounter = new FPSCounter(s =>
			{
			});
			base.Updatables.Add(m_fpsCounter);
			

			//m_testCubeRenderer = new TestCubeRenderer(this.GraphicsDevice, m_camera);
			//base.Updatables.Add(m_testCubeRenderer);



			GameData.Data.UserConnected += Data_UserConnected;
			GameData.Data.UserDisconnected += Data_UserDisconnected;

			GameData.Data.MapChanged += Data_MapChanged;



			this.RasterizerState = this.GraphicsDevice.RasterizerStates.CullBack;



			var mainView = new GameSurfaceView(this.GraphicsDevice)
			{
				Camera = m_camera,
			};
			mainView.Drawables.Add(m_terrainRenderer);
			mainView.Drawables.Add(m_debugAxesRenderer);
			//mainView.Drawables.Add(m_testCubeRenderer);
			//mainView.Drawables.Add(m_outlineRenderer);
			mainView.Drawables.Add(m_selectionRenderer);
			
			// surface

			var surface = ToDispose(new GameSurface(this.GraphicsDevice, mainHost));

			surface.Views.Add(mainView);

			surface.SizeChanged += (w, h) =>
			{
				var vp = new ViewportF(0, 0, w, h);
				mainView.ViewPort = vp;
				mainView.Camera.SetAspect(vp.AspectRatio);
			};

			base.Surfaces.Add(surface);

		}

		void Data_UserDisconnected()
		{
			GameData.Data.Map = null;
		}

		void Data_UserConnected()
		{
			var data = GameData.Data;

			if (data.GameMode == GameMode.Adventure)
				GoTo(data.FocusedObject);
			else
				GoTo(data.World.Controllables.First());
		}

		void GoTo(LivingObject ob)
		{
			var env = ob.Environment;
			GameData.Data.Map = env;
		}

		void Data_MapChanged(EnvironmentObject oldMap, EnvironmentObject newMap)
		{
			var map = GameData.Data.Map;

			if (map != null)
			{
				var pos = map.Size.ToIntVector3().ToVector3();
				pos.X /= 2;
				pos.Y /= 2;
				pos.Z += 10;

				var target = new Vector3(0, 0, 0);

				//pos = new Vector3(-5, -4, 32); target = new Vector3(40, 40, 0);

				m_camera.LookAt(pos, target, Vector3.UnitZ);
			}

			//m_terrainRenderer.Enabled = map != null;
			m_selectionRenderer.IsEnabled = map != null;
		}
	}
}
