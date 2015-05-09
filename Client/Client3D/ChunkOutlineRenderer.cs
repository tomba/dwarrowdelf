using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;

namespace Dwarrowdelf.Client
{
	sealed class ChunkOutlineRenderer : GameComponent
	{
		GeometricPrimitive m_cube;
		Texture2D m_cubeTexture;

		BasicEffect m_basicEffect;

		ChunkManager m_chunkManager;

		RasterizerState m_rasterizerState;

		public ChunkOutlineRenderer(MyGame game, ChunkManager chunkManager)
			: base(game)
		{
			m_chunkManager = chunkManager;

			LoadContent();
		}

		void LoadContent()
		{
			var device = this.GraphicsDevice;

			m_basicEffect = ToDispose(new BasicEffect(GraphicsDevice));

			m_basicEffect.TextureEnabled = true;
			m_basicEffect.Sampler = device.SamplerStates.PointClamp;

			LoadCube();

			var rdesc = device.RasterizerStates.Default.Description;
			rdesc.DepthBias = -10;
			m_rasterizerState = RasterizerState.New(device, rdesc);
		}

		public override void Update(TimeSpan gameTime)
		{
		}

		public override void Draw(Camera camera)
		{
			m_basicEffect.View = camera.View;
			m_basicEffect.Projection = camera.Projection;

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
			m_cube = ToDispose(GeometricPrimitive.Cube.New(GraphicsDevice, Chunk.CHUNK_SIZE, toLeftHanded: true));

			m_cubeTexture = this.Content.Load<Texture2D>("RectangleOutline");
		}
	}
}
