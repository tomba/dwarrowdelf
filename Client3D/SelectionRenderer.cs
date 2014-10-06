using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Runtime.InteropServices;
using System.Linq;
using Dwarrowdelf;
using System.Collections.Generic;
using SharpDX.Toolkit.Input;
using System.Diagnostics;

namespace Client3D
{
	sealed class SelectionRenderer : GameSystem
	{
		MouseManager m_mouseManager;
		CameraProvider m_cameraService;

		Effect m_effect;

		VertexInputLayout m_layout;
		Buffer<VertexPositionColorTexture> m_vertexBuffer;

		bool SelectionVisible { get; set; }
		IntVector3 SelectionStart { get; set; }
		IntVector3 SelectionEnd { get; set; }
		Direction SelectionDirection { get; set; }

		bool CursorVisible { get; set; }
		IntVector3 Position { get; set; }
		Direction Direction { get; set; }

		public SelectionRenderer(Game game, MouseManager mouseManager)
			: base(game)
		{
			m_mouseManager = mouseManager;

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

			var tex = new[] {
				new Vector2(1, 0),
				new Vector2(1, 1),
				new Vector2(0, 1),
				new Vector2(0, 0),
			};

			var vertices = new List<VertexPositionColorTexture>();
			for (int i = 0; i < 6; ++i)
			{
				var color = i == (int)DirectionOrdinal.South ? new Color(255, 255, 255) : new Color(128, 128, 128);

				var ver = Chunk.s_cubeFaceInfo[i].Vertices.Select(v => v.ToVector3())
					.ToArray();

				var vs = new List<VertexPositionColorTexture>();
				vs.Add(new VertexPositionColorTexture(ver[0], color, tex[0]));
				vs.Add(new VertexPositionColorTexture(ver[1], color, tex[1]));
				vs.Add(new VertexPositionColorTexture(ver[2], color, tex[2]));

				vs.Add(new VertexPositionColorTexture(ver[2], color, tex[2]));
				vs.Add(new VertexPositionColorTexture(ver[3], color, tex[3]));
				vs.Add(new VertexPositionColorTexture(ver[0], color, tex[0]));

				vertices.AddRange(vs);
			}

			m_layout = VertexInputLayout.New<VertexPositionColorTexture>(0);

			m_vertexBuffer = ToDispose(Buffer.Vertex.New<VertexPositionColorTexture>(device, vertices.ToArray()));
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

		void SetWorlMatrix(IntVector3 pos, IntSize3 size, Direction dir)
		{
			var worldMatrix = Matrix.Identity;
			worldMatrix.Transpose();

			worldMatrix *= Matrix.Translation(new Vector3(-0.5f));
			worldMatrix *= Matrix.RotationQuaternion(s_rotationQuaternions[(int)dir.ToDirectionOrdinal()]);
			worldMatrix *= Matrix.Scaling(size.Width, size.Height, size.Depth);
			worldMatrix *= Matrix.Scaling(new Vector3(0.01f) / size.ToIntVector3().ToVector3() + 1); // fix z fight
			worldMatrix *= Matrix.Translation(new Vector3(0.5f));
			worldMatrix *= Matrix.Translation((size.ToIntVector3().ToVector3() - new Vector3(1)) / 2);
			worldMatrix *= Matrix.Translation(pos.ToVector3());

			m_effect.Parameters["worldMatrix"].SetValue(ref worldMatrix);
		}

		public override void Update(GameTime gameTime)
		{
			var viewPort = this.GraphicsDevice.Viewport;

			if (viewPort.Bounds.IsEmpty == false)
			{
				var mouseState = m_mouseManager.GetState();

				var mousePos = new IntVector2(MyMath.Round(mouseState.X * viewPort.Width), MyMath.Round(mouseState.Y * viewPort.Height));

				IntVector3 p;
				Direction d;

				bool hit = ((MyGame)this.Game).MousePickVoxel(mousePos, out p, out d);

				// cursor

				if (hit)
				{
					if (mouseState.LeftButton.Pressed)
					{
						var vx = GlobalData.VoxelMap.GetVoxel(p);

						System.Diagnostics.Trace.TraceInformation("pick: {0} face: {1}, voxel: ({2})", p, d, vx);
					}

					this.Position = p;
					this.Direction = d;
					this.CursorVisible = true;
				}
				else
				{
					this.CursorVisible = false;
				}

				// selection
				if (hit)
				{
					if (mouseState.LeftButton.Pressed)
					{
						this.SelectionVisible = true;
						this.SelectionStart = p;
						this.SelectionDirection = d;
					}

					if (this.SelectionVisible)
					{
						this.SelectionEnd = p;
					}
				}

				if (mouseState.LeftButton.Released && this.SelectionVisible)
				{
					this.SelectionVisible = false;

					Trace.TraceError("Select {0}, {1}", this.SelectionStart, this.SelectionEnd);
				}
			}

			if (this.CursorVisible == false && this.SelectionVisible == false)
				return;

			var device = this.Game.GraphicsDevice;

			var viewProjMatrix = Matrix.Transpose(m_cameraService.View * m_cameraService.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			if (this.CursorVisible == false && this.SelectionVisible == false)
				return;

			base.Draw(gameTime);

			var device = this.Game.GraphicsDevice;

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetBlendState(device.BlendStates.NonPremultiplied);
			device.SetDepthStencilState(device.DepthStencilStates.DepthRead);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			device.SetVertexBuffer(m_vertexBuffer);
			device.SetVertexInputLayout(m_layout);

			if (this.CursorVisible)
			{
				m_effect.Parameters["s_cubeColor"].SetValue(Color.Red.ToVector3());
				SetWorlMatrix(this.Position, new IntSize3(1, 1, 1), this.Direction);
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}

			if (this.SelectionVisible)
			{
				var grid = new IntGrid3(this.SelectionStart, this.SelectionEnd);
				SetWorlMatrix(grid.Corner1, grid.Size, this.SelectionDirection);

				m_effect.Parameters["s_cubeColor"].SetValue(Color.Blue.ToVector3());
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}
		}
	}
}
