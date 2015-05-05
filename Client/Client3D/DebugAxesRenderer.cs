using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;

namespace Dwarrowdelf.Client
{
	sealed class DebugAxesRenderer : GameSystem
	{
		CameraProvider m_cameraService;

		BasicEffect m_basicEffect;

		PrimitiveBatch<VertexPositionColor> m_batch;
		GeometricPrimitive m_plane;

		public DebugAxesRenderer(Game game)
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

			m_basicEffect.VertexColorEnabled = true;
			m_basicEffect.LightingEnabled = false;

			m_batch = new PrimitiveBatch<VertexPositionColor>(this.GraphicsDevice);
			m_plane = ToDisposeContent(GeometricPrimitive.Plane.New(this.GraphicsDevice, 2, 2, 1, true));
		}

		public override void Update(GameTime gameTime)
		{
		}

		public override void Draw(GameTime gameTime)
		{
			DrawPlane();
			DrawAxes();
		}

		void DrawAxes()
		{
			var device = this.GraphicsDevice;

			var campos = m_cameraService.Position;
			var look = m_cameraService.Look;

			var pos = campos + look * 15;

			m_basicEffect.World = Matrix.Identity * Matrix.Translation(pos);
			m_basicEffect.View = m_cameraService.View;
			m_basicEffect.Projection = m_cameraService.Projection * Matrix.Translation(0.9f, 0.9f, 0);

			m_basicEffect.TextureEnabled = false;
			m_basicEffect.VertexColorEnabled = true;

			m_basicEffect.Alpha = 1;

			m_basicEffect.CurrentTechnique.Passes[0].Apply();

			const int mul = 1;

			device.SetDepthStencilState(device.DepthStencilStates.None);
			device.SetRasterizerState(device.RasterizerStates.CullNone);

			m_batch.Begin();
			m_batch.DrawLine(new VertexPositionColor(new Vector3(0, 0, 0), Color.Red),
				new VertexPositionColor(Vector3.UnitX * mul, Color.Red));
			m_batch.DrawLine(new VertexPositionColor(new Vector3(0, 0, 0), Color.Green),
				new VertexPositionColor(Vector3.UnitY * mul, Color.Green));
			m_batch.DrawLine(new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue),
				new VertexPositionColor(Vector3.UnitZ * mul, Color.Blue));
			m_batch.End();

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}

		void DrawPlane()
		{
			var device = this.GraphicsDevice;

			var campos = m_cameraService.Position;
			var look = m_cameraService.Look;

			var pos = campos + look * 10;

			m_basicEffect.World = Matrix.Identity;
			m_basicEffect.View = Matrix.Identity * Matrix.Translation(0, 0, 15);
			m_basicEffect.Projection = m_cameraService.Projection * Matrix.Translation(0.9f, 0.9f, 0);

			m_basicEffect.TextureEnabled = false;
			m_basicEffect.VertexColorEnabled = false;

			m_basicEffect.Alpha = 0.8f;
			m_basicEffect.DiffuseColor = new Vector4(1f, 1f, 1f, 1);

			device.SetBlendState(device.BlendStates.NonPremultiplied);

			device.SetDepthStencilState(device.DepthStencilStates.None);
			device.SetRasterizerState(device.RasterizerStates.CullNone);

			m_basicEffect.CurrentTechnique.Passes[0].Apply();
			m_plane.Draw(m_basicEffect);

			device.SetBlendState(device.BlendStates.Default);
			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}
	}
}
