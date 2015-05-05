using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace Dwarrowdelf.Client
{
	sealed class ChunkOutlineRenderer : GameSystem
	{
		CameraProvider m_cameraService;

		GeometricPrimitive m_cube;
		Texture2D m_cubeTexture;

		BasicEffect m_basicEffect;

		ChunkManager m_chunkManager;

		RasterizerState m_rasterizerState;

		public ChunkOutlineRenderer(Game game, ChunkManager chunkManager)
			: base(game)
		{
			m_chunkManager = chunkManager;

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
			var device = this.GraphicsDevice;

			base.LoadContent();

			m_basicEffect = ToDisposeContent(new BasicEffect(GraphicsDevice));

			m_basicEffect.TextureEnabled = true;
			m_basicEffect.Sampler = device.SamplerStates.PointClamp;

			LoadCube();

			var rdesc = device.RasterizerStates.Default.Description;
			rdesc.DepthBias = -10;
			m_rasterizerState = RasterizerState.New(device, rdesc);
		}

		public override void Update(GameTime gameTime)
		{
			var time = (float)gameTime.TotalGameTime.TotalSeconds;

			m_basicEffect.View = m_cameraService.View;
			m_basicEffect.Projection = m_cameraService.Projection;
		}

		public override void Draw(GameTime gameTime)
		{
			var chunks = m_chunkManager.DebugChunks;

			m_basicEffect.Texture = m_cubeTexture;

			var device = this.GraphicsDevice;
			device.SetBlendState(device.BlendStates.NonPremultiplied);

			device.SetRasterizerState(m_rasterizerState);

			foreach (var cp in m_chunkManager.Size.Range())
			{
				var chunk = chunks[m_chunkManager.Size.GetIndex(cp)];
				if (chunk == null)
					continue;

				if (chunk.IsAllEmpty)
					m_basicEffect.DiffuseColor = new Vector4(1, 0, 0, 0);
				else if (chunk.IsAllUndefined)
					m_basicEffect.DiffuseColor = new Vector4(0, 1, 0, 0);
				else
					m_basicEffect.DiffuseColor = new Vector4(1, 1, 1, 0);

				m_basicEffect.World = Matrix.Translation((cp * Chunk.CHUNK_SIZE + Chunk.CHUNK_SIZE / 2).ToVector3());
				m_cube.Draw(m_basicEffect);
			}

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetBlendState(device.BlendStates.Default);
		}

		private void LoadCube()
		{
			m_cube = ToDisposeContent(GeometricPrimitive.Cube.New(GraphicsDevice, Chunk.CHUNK_SIZE, toLeftHanded: true));

			m_cubeTexture = Content.Load<Texture2D>("RectangleOutline");
		}
	}
}
