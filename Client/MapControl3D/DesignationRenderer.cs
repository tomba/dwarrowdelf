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
	sealed class DesignationRenderer : GameComponent
	{
		Effect m_effect;

		VertexInputLayout m_layout;
		Buffer<VertexPositionColorTexture> m_vertexBuffer;

		MyGame m_game;

		public DesignationRenderer(MyGame game)
			: base(game)
		{
			m_game = game;
			//m_game.EnvironmentChanged += OnEnvChanged;

			LoadContent();
		}

		void OnEnvChanged(EnvironmentObject oldEnv, EnvironmentObject newEnv)
		{

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

		static DesignationRenderer()
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

		readonly Color m_color1 = Color.White;
		readonly Color m_color2 = Color.Gray;

		public override void Update()
		{
		}

		public override void Draw(Camera camera)
		{
			if (m_game.Environment == null)
				return;

			var des = m_game.Environment.Designations;

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

			float t = (float)(Math.Sin(m_game.Time.TotalTime.TotalSeconds * 4) + 1) / 2;

			var color = Color.Lerp(m_color1, m_color2, t);

#warning TODO: drawn one cube at a time
#warning TODO: combine code with SelectionRenderer

			m_effect.Parameters["s_cubeColor"].SetValue(color.ToVector3());

			var designations = des.GetLocations().Where(kvp => m_game.ViewGridProvider.ViewGrid.Contains(kvp.Key));

			foreach (var kvp in designations)
			{
				SetWorlMatrix(kvp.Key, new IntSize3(1, 1, 1), Direction.Up);
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}

			device.SetBlendState(device.BlendStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}
	}
}
