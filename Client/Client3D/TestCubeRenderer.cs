using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace Client3D
{
	sealed class TestCubeRenderer : GameSystem
	{
		CameraProvider m_cameraService;

		GeometricPrimitive m_cube;
		Texture2D m_cubeTexture;
		Matrix m_cubeTransform;

		BasicEffect m_basicEffect;

		public TestCubeRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			game.GameSystems.Add(this);
		}

		public override void Initialize()
		{
			base.Initialize();

			m_cameraService = this.Services.GetService<CameraProvider>();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			m_basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));

			m_basicEffect.EnableDefaultLighting(); // enable default lightning, useful for quick prototyping
			m_basicEffect.TextureEnabled = true;   // enable texture drawing

			LoadCube();
		}

		public override void Update(GameTime gameTime)
		{
			var time = (float)gameTime.TotalGameTime.TotalSeconds;

			m_cubeTransform = Matrix.RotationX(time) * Matrix.RotationY(time * 2f) * Matrix.RotationZ(time * .7f) *
				Matrix.Translation(m_cameraService.Position + m_cameraService.Look * 10);

			m_basicEffect.View = m_cameraService.View;
			m_basicEffect.Projection = m_cameraService.Projection;
		}

		public override void Draw(GameTime gameTime)
		{
			m_basicEffect.Texture = m_cubeTexture;
			m_basicEffect.World = m_cubeTransform;
			m_cube.Draw(m_basicEffect);
		}

		void LoadCube()
		{
			m_cube = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice, 1, toLeftHanded: true));

			m_cubeTexture = Content.Load<Texture2D>("logo_large");

			m_cubeTransform = Matrix.Identity;
		}
	}
}
