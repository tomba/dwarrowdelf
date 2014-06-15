using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace Client3D
{
	/// <summary>
	/// Component responsible for the scene rendering.
	/// </summary>
	sealed class SceneRenderer : GameSystem
	{
		private ICameraService m_cameraService;

		private GeometricPrimitive m_cube;
		private Texture2D m_cubeTexture;
		private Matrix m_cubeTransform;

		private GeometricPrimitive m_plane;
		private Texture2D m_planeTexture;
		private Matrix m_planeTransform;

		private BasicEffect m_basicEffect;

		/// <summary>
		/// Initialize in constructor anything that doesn't depend on other services.
		/// </summary>
		/// <param name="game">The game where this system will be attached to.</param>
		public SceneRenderer(Game game)
			: base(game)
		{
			// this game system has something to draw - enable drawing by default
			// this can be disabled to make objects drawn by this system disappear
			this.Visible = true;

			// this game system has logic that needs to be updated - enable update by default
			// this can be disabled to simulate a "pause" in logic update
			this.Enabled = true;

			// add the system itself to the systems list, so that it will get initialized and processed properly
			// this can be done after game initialization - the Game class supports adding and removing of game systems dynamically
			game.GameSystems.Add(this);
		}

		/// <summary>
		/// Initialize here anything that depends on other services
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// get the camera service from service registry
			m_cameraService = Services.GetService<ICameraService>();
		}

		/// <summary>
		/// Load all graphics content here.
		/// </summary>
		protected override void LoadContent()
		{
			base.LoadContent();

			// initialize the basic effect (shader) to draw the geometry, the BasicEffect class is similar to one from XNA
			m_basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));

			m_basicEffect.EnableDefaultLighting(); // enable default lightning, useful for quick prototyping
			m_basicEffect.TextureEnabled = true;   // enable texture drawing

			LoadCube();

			LoadPlane();
		}

		/// <summary>
		/// Draw the scene content.
		/// </summary>
		/// <param name="gameTime">Structure containing information about elapsed game time.</param>
		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			// set the parameters for cube drawing and draw it using the basic effect
			m_basicEffect.Texture = m_cubeTexture;
			m_basicEffect.World = m_cubeTransform;
			m_cube.Draw(m_basicEffect);

			// set the parameters for plane drawing and draw it using the basic effect
			m_basicEffect.Texture = m_planeTexture;
			m_basicEffect.World = m_planeTransform;
			m_plane.Draw(m_basicEffect);
		}

		/// <summary>
		/// Update the scene logic.
		/// </summary>
		/// <param name="gameTime">Structure containing information about elapsed game time.</param>
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// get the total elapsed seconds since the start of the game
			var time = (float)gameTime.TotalGameTime.TotalSeconds;

			// update the cube position to add some movement
			m_cubeTransform = Matrix.RotationX(time) * Matrix.RotationY(time * 2f) * Matrix.RotationZ(time * .7f);

			// update view and projection matrices from the camera service
			m_basicEffect.View = m_cameraService.View;
			m_basicEffect.Projection = m_cameraService.Projection;
		}

		/// <summary>
		/// Loads cube geometry and anything related.
		/// </summary>
		private void LoadCube()
		{
			// build the cube geometry and mark it disposable when content will be uploaded
			m_cube = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice, 1, toLeftHanded: true));

			// load the texture using game's content manager
			m_cubeTexture = Content.Load<Texture2D>("logo_large");

			// the cube's transform will be updated in runtime
			m_cubeTransform = Matrix.Identity;
		}

		/// <summary>
		/// Loads plane geometry and anything related.
		/// </summary>
		private void LoadPlane()
		{
			// build the plane geometry of the specified size and subdivision segments
			m_plane = ToDisposeContent(GeometricPrimitive.Plane.New(GraphicsDevice, 50f, 50f, toLeftHanded: true));

			// load the texture using game's content manager
			m_planeTexture = Content.Load<Texture2D>("GeneticaMortarlessBlocks");

			// move it down a bit
			m_planeTransform = Matrix.Translation(0f, 0f, -5f);
		}
	}
}
