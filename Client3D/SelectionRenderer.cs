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

		public bool SelectionEnabled { get; set; }
		public IntVector3 SelectionStart { get; set; }
		public IntVector3 SelectionEnd { get; set; }

		public bool CursorVisible { get; set; }
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

		static readonly Quaternion[] s_rotationQuaternions;

		static SelectionRenderer()
		{
			s_rotationQuaternions = new Quaternion[6];
			s_rotationQuaternions[(int)DirectionOrdinal.West] = Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.PiOverTwo);
			s_rotationQuaternions[(int)DirectionOrdinal.East] = Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo);
			s_rotationQuaternions[(int)DirectionOrdinal.North] = Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.Pi);
			s_rotationQuaternions[(int)DirectionOrdinal.South] = Quaternion.RotationAxis(Vector3.UnitZ, 0);
			s_rotationQuaternions[(int)DirectionOrdinal.Down] = Quaternion.RotationAxis(Vector3.UnitX, -MathUtil.PiOverTwo);
			s_rotationQuaternions[(int)DirectionOrdinal.Up] = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (this.CursorVisible == false)
				return;

			var viewProjMatrix = Matrix.Transpose(m_cameraService.View * m_cameraService.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

			var worldMatrix = Matrix.Identity;
			worldMatrix.Transpose();
			worldMatrix *= Matrix.RotationQuaternion(s_rotationQuaternions[(int)this.Direction.ToDirectionOrdinal()]);
			worldMatrix *= Matrix.Translation(this.Position.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
			worldMatrix *= Matrix.Translation(this.Direction.ToIntVector3().ToVector3() / 2.0f);
			m_effect.Parameters["worldMatrix"].SetValue(ref worldMatrix);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (this.CursorVisible == false)
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
