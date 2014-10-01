using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Runtime.InteropServices;

namespace Client3D
{
	sealed class TestRenderer : GameSystem
	{
		CameraProvider m_cameraService;

		Effect m_effect;

		Buffer<MyVertex> m_vertexBuffer;
		VertexInputLayout m_layout;

		public TestRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			game.GameSystems.Add(this);
		}

		public override void Initialize()
		{
			base.Initialize();

			m_cameraService = Services.GetService<CameraProvider>();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			m_effect = this.Content.Load<Effect>("TestEffect");

			var vertices = new MyVertex[] {
				new MyVertex(new Vector3(0, 0, 0), Color.Green),
				new MyVertex(new Vector3(1, 0, 0), Color.Red),
				new MyVertex(new Vector3(0, 1, 0), Color.Red),
				new MyVertex(new Vector3(1, 1, 0), Color.Red),
			};

			var device = this.Game.GraphicsDevice;

			m_vertexBuffer = Buffer.New<MyVertex>(device, vertices, BufferFlags.VertexBuffer);
			m_layout = VertexInputLayout.FromBuffer(0, m_vertexBuffer);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			var viewProjMatrix = Matrix.Transpose(m_cameraService.View * m_cameraService.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

			var worldMatrix = Matrix.Identity;
			worldMatrix.Transpose();
			m_effect.Parameters["worldMatrix"].SetValue(ref worldMatrix);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);


			this.GraphicsDevice.SetRasterizerState(this.GraphicsDevice.RasterizerStates.Default);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			var device = this.Game.GraphicsDevice;

			device.SetVertexBuffer(m_vertexBuffer);
			device.SetVertexInputLayout(m_layout);
			device.Draw(PrimitiveType.LineListWithAdjacency, 4);
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct MyVertex
		{
			[VertexElement("POSITION")]
			public Vector3 Position;
			[VertexElement("COLOR0")]
			public Vector4 Color;

			public MyVertex(Vector3 pos, Color color)
			{
				this.Position = pos;
				this.Color = color.ToVector4();
			}
		}
	}
}
