using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Runtime.InteropServices;
using System.Linq;
using Dwarrowdelf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System;

namespace Dwarrowdelf.Client
{
	sealed class SelectionRenderer : GameComponent
	{
		Effect m_effect;

		VertexInputLayout m_layout;
		Buffer<VertexPositionColorTexture> m_vertexBuffer;

		public bool IsEnabled { get; set; }

		MyGame m_game;

		public SelectionRenderer(MyGame game)
			: base(game)
		{
			m_game = game;

			LoadContent();

			this.IsEnabled = false;
		}

		void LoadContent()
		{
			var device = this.GraphicsDevice;

			m_effect = this.Content.Load<Effect>("SelectionEffect");

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

			m_vertexBuffer = ToDispose(SharpDX.Toolkit.Graphics.Buffer.Vertex.New<VertexPositionColorTexture>(device, vertices.ToArray()));
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

		public override void Update()
		{
		}

		public override void Draw(Camera camera)
		{
			if (!this.IsEnabled)
				return;

			IntVector3? cursorPos = null;

			if (m_game.CursorService.IsEnabled)
				cursorPos = m_game.CursorService.Location;

			var selection = m_game.SelectionService.Selection;
			var selDir = Direction.Up; // XXX

			if (cursorPos.HasValue == false && selection.IsSelectionValid == false)
				return;

			var device = this.GraphicsDevice;

			var viewProjMatrix = Matrix.Transpose(camera.View * camera.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

			device.SetBlendState(device.BlendStates.NonPremultiplied);
			device.SetDepthStencilState(device.DepthStencilStates.DepthRead);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			device.SetVertexBuffer(m_vertexBuffer);
			device.SetVertexInputLayout(m_layout);

			if (cursorPos.HasValue)
			{
				m_effect.Parameters["s_cubeColor"].SetValue(Color.Red.ToVector3());
				SetWorlMatrix(cursorPos.Value, new IntSize3(1, 1, 1), Direction.Up);
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}

			if (selection.IsSelectionValid)
			{
				var grid = selection.SelectionBox;
				SetWorlMatrix(grid.Corner1, grid.Size, selDir);

				m_effect.Parameters["s_cubeColor"].SetValue(Color.Blue.ToVector3());
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}

			device.SetBlendState(device.BlendStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}
	}
}
