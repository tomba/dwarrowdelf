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
		Camera m_cameraService;
		ViewGridProvider m_viewGridProvider;
		SharpDXHost m_control;

		Effect m_effect;

		VertexInputLayout m_layout;
		Buffer<VertexPositionColorTexture> m_vertexBuffer;

		public bool SelectionVisible { get; private set; }
		public IntVector3 SelectionStart { get; private set; }
		public IntVector3 SelectionEnd { get; private set; }
		public IntGrid3 SelectionGrid { get { return new IntGrid3(this.SelectionStart, SelectionEnd); } }
		public Direction SelectionDirection { get; private set; }

		public bool CursorVisible { get; private set; }
		public IntVector3 Position { get; private set; }
		public Direction Direction { get; private set; }

		public bool IsEnabled { get; set; }

		bool m_isDown;
		bool m_isClick;

		public SelectionRenderer(MyGame game, Camera camera, ViewGridProvider viewGridProvider, SharpDXHost control)
			: base(game)
		{
			m_cameraService = camera;
			m_viewGridProvider = viewGridProvider;
			m_control = control;

			LoadContent();

			this.IsEnabled = false;

			control.MouseLeftButtonDown += control_MouseLeftButtonDown;
			control.MouseLeftButtonUp += control_MouseLeftButtonUp;
		}

		void control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (m_isDown == false)
				return;

			m_isDown = false;
			m_isClick = true;
		}

		void control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			m_isDown = true;
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

		bool MousePickVoxel(IntVector2 mousePos, out IntVector3 pos, out Direction face)
		{
			var camera = m_cameraService;

			// XXX viewport wrong

			var ray = Ray.GetPickRay(mousePos.X, mousePos.Y, this.GraphicsDevice.Viewport, camera.View * camera.Projection);

			IntVector3 outpos = new IntVector3();
			Direction outdir = Direction.None;

			var viewGrid = m_viewGridProvider.ViewGrid;

			VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
				(x, y, z, dir) =>
				{
					var p = new IntVector3(x, y, z);

					if (viewGrid.Contains(p) == false)
						return false;

					var td = GameData.Data.Map.GetTileData(p);

					if (td.IsEmpty)
						return false;

					outpos = p;
					outdir = dir;

					return true;
				});

			pos = outpos;
			face = outdir;
			return face != Direction.None;
		}

		public override void Update(TimeSpan gameTime)
		{
			if (!this.IsEnabled)
				return;

			HandleMouse();

			m_isClick = false;
		}

		void HandleMouse()
		{
			var pos = Mouse.GetPosition(m_control);

			var mousePos = new IntVector2(MyMath.Round(pos.X), MyMath.Round(pos.Y));

			IntVector3 p;
			Direction d;

			bool hit = MousePickVoxel(mousePos, out p, out d);

			// cursor

			if (hit)
			{
				if (m_isClick)
				{
					var td = GameData.Data.Map.GetTileData(p);

					System.Diagnostics.Trace.TraceInformation("pick: {0} face: {1}, voxel: ({2})", p, d, td);
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
				if (m_isDown)
				{
					if (this.SelectionVisible == false)
					{
						this.SelectionVisible = true;
						this.SelectionStart = p;
						this.SelectionDirection = d;
					}

					this.SelectionEnd = p;
				}
			}

			if (this.SelectionVisible && m_isDown == false)
			{
				this.SelectionVisible = false;

				Trace.TraceError("Select {0}, {1}", this.SelectionStart, this.SelectionEnd);
			}
		}

		public override void Draw(Camera camera)
		{
			if (!this.IsEnabled)
				return;

			if (this.CursorVisible == false && this.SelectionVisible == false)
				return;

			var device = this.GraphicsDevice;

			var viewProjMatrix = Matrix.Transpose(m_cameraService.View * m_cameraService.Projection);
			viewProjMatrix.Transpose();
			m_effect.Parameters["viewProjMatrix"].SetValue(ref viewProjMatrix);

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
				var grid = this.SelectionGrid;
				SetWorlMatrix(grid.Corner1, grid.Size, this.SelectionDirection);

				m_effect.Parameters["s_cubeColor"].SetValue(Color.Blue.ToVector3());
				m_effect.ConstantBuffers["PerObject"].Update();

				device.Draw(PrimitiveType.TriangleList, 6 * 6);
			}

			device.SetBlendState(device.BlendStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}
	}
}
