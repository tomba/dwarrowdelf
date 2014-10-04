using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Runtime.InteropServices;
using System.Linq;
using Dwarrowdelf;

namespace Client3D
{
	sealed class SelectionRenderer : GameSystem
	{
		CameraProvider m_cameraService;

		Effect m_effect;

		Buffer<VertexPositionColor> m_vertexBuffer;
		VertexInputLayout m_layout;

		public bool CursorEnabled { get; set; }
		public IntVector3 Position { get; set; }
		public Direction Direction { get; set; }

		public SelectionRenderer(Game game)
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

			m_effect = ToDispose(this.Content.Load<Effect>("SelectionEffect"));

			var device = this.Game.GraphicsDevice;

			// south face
			var ver = new[] {
				new Vector3(0,1,1),
				new Vector3(1,1,1),
				new Vector3(0,1,0),
				new Vector3(1,1,0),
			};

			var vertices = ver
				.Select(v => v + new Vector3(-0.5f, -1f, -0.5f))
				.Select(v => new VertexPositionColor(v, Color.Blue))
				.ToArray();

			m_vertexBuffer = ToDispose(Buffer.Vertex.New<VertexPositionColor>(device, vertices));
			m_layout = VertexInputLayout.FromBuffer(0, m_vertexBuffer);
		}

		static Quaternion[] s_rotationQuaternions = new[] {
			Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo),
			Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.PiOverTwo),
			Quaternion.RotationAxis(Vector3.UnitZ, 0),
			Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.Pi),
			Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo),
			Quaternion.RotationAxis(Vector3.UnitX, -MathUtil.PiOverTwo),
		};

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (this.CursorEnabled == false)
				return;

			var viewProjMatrix = Matrix.Transpose(m_cameraService.View * m_cameraService.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

			var worldMatrix = Matrix.Identity;
			worldMatrix.Transpose();
			worldMatrix *= Matrix.RotationQuaternion(s_rotationQuaternions[(int)this.Direction.ToFaceDirection()]);
			worldMatrix *= Matrix.Translation(this.Position.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
			worldMatrix *= Matrix.Translation(new IntVector3(this.Direction).ToVector3() / 2.0f);
			m_effect.Parameters["worldMatrix"].SetValue(ref worldMatrix);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (this.CursorEnabled == false)
				return;

			var device = this.Game.GraphicsDevice;

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetBlendState(device.BlendStates.NonPremultiplied);
			device.SetDepthStencilState(device.DepthStencilStates.None);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			device.SetVertexBuffer(m_vertexBuffer);
			device.SetVertexInputLayout(m_layout);
			device.Draw(PrimitiveType.TriangleStrip, 4);
		}
	}
}
