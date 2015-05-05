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

namespace Dwarrowdelf.Client
{
	sealed class MyGame : Game
	{
		readonly GraphicsDeviceManager m_graphicsDeviceManager;
		readonly CameraProvider m_cameraProvider;
		readonly KeyboardHandler m_keyboardHandler;
		readonly MouseManager m_mouseManager;
		readonly ViewGridProvider m_viewGridProvider;
		readonly TerrainRenderer m_terrainRenderer;
		readonly SymbolRenderer m_symbolRenderer;
		readonly SelectionRenderer m_selectionRenderer;
		readonly FPSCounterSystem m_fpsCounter;

		readonly TestCubeRenderer m_testCubeRenderer;
		readonly DebugAxesRenderer m_debugAxesRenderer;
		readonly ChunkOutlineRenderer m_outlineRenderer;

		public GraphicsDeviceManager GraphicsDeviceManager { get { return m_graphicsDeviceManager; } }
		public TerrainRenderer TerrainRenderer { get { return m_terrainRenderer; } }
		public RasterizerState RasterizerState { get; set; }

		public MyGame()
		{
			this.IsMouseVisible = true;
			m_graphicsDeviceManager = new GraphicsDeviceManager(this);

			//this.GameSystems.Add(new EffectCompilerSystem(this));		// allows changing shaders runtime
			m_cameraProvider = new CameraProvider(this);
			m_keyboardHandler = new KeyboardHandler(this);
			m_mouseManager = new MouseManager(this);

			m_viewGridProvider = new ViewGridProvider(this);

			m_terrainRenderer = new TerrainRenderer(this);
			//m_symbolRenderer = new SymbolRenderer(this, m_movableManager);
			m_selectionRenderer = new SelectionRenderer(this, m_mouseManager);
			m_debugAxesRenderer = new DebugAxesRenderer(this);
			//m_outlineRenderer = new ChunkOutlineRenderer(this, m_terrainRenderer.ChunkManager);

			m_fpsCounter = new FPSCounterSystem(this, s => this.Window.Title = s);

			//m_testCubeRenderer = new TestCubeRenderer(this);

			Content.RootDirectory = "Content";

			GameData.Data.MapChanged += Data_MapChanged;

			GameData.Data.Map = new Map();
		}

		void Data_MapChanged()
		{
			var map = GameData.Data.Map;

			if (map == null)
				return;

			var pos = map.Size.ToIntVector3().ToVector3();
			pos.X /= 2;
			pos.Y /= 2;
			pos.Z += 10;

			var target = new Vector3(0, 0, 0);

			//pos = new Vector3(-5, -4, 32); target = new Vector3(40, 40, 0);

			m_cameraProvider.LookAt(pos, target, Vector3.UnitZ);
		}

		protected override void OnWindowCreated()
		{
			base.OnWindowCreated();

			FixSharpDXWindowSizeBug();
		}

		void FixSharpDXWindowSizeBug()
		{
			// sharpdx backbuffer is initialized with wrong size for some reason, so we need to resize then window
			// to get the proper size

			var ctrl = this.Window.NativeWindow as System.Windows.Forms.Control;
			if (ctrl == null)
				return;

			var form = (System.Windows.Forms.Form)ctrl.TopLevelControl;
			form.Height -= 1;
			form.Height += 1;
		}

		protected override void Initialize()
		{
			base.Initialize();

			m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.Window.ClientSizeChanged += (s, e) =>
					m_cameraProvider.SetAspect((float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height);

			this.RasterizerState = this.GraphicsDevice.RasterizerStates.CullBack;
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			this.GraphicsDevice.Clear(Color.CornflowerBlue);

			base.Draw(gameTime);
		}
	}
}
