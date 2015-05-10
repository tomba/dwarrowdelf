using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;

namespace Dwarrowdelf.Client
{
	sealed class DebugAxesRenderer : GameComponent
	{
		BasicEffect m_basicEffect;

		GeometricPrimitive m_plane;

		Buffer<VertexPositionColor> m_buffer;
		VertexInputLayout m_layout;

		readonly VertexPositionColor[] m_vertices = new[]{
			new VertexPositionColor(new Vector3(), Color.Red),
			new VertexPositionColor(Vector3.UnitX, Color.Red),

			new VertexPositionColor(new Vector3(), Color.Green),
			new VertexPositionColor(Vector3.UnitY, Color.Green),

			new VertexPositionColor(new Vector3(), Color.Blue),
			new VertexPositionColor(Vector3.UnitZ, Color.Blue),
		};

		public DebugAxesRenderer(MyGame game)
			: base(game)
		{
			LoadContent();
		}

		void LoadContent()
		{
			m_basicEffect = ToDispose(new BasicEffect(this.GraphicsDevice));

			m_basicEffect.VertexColorEnabled = true;
			m_basicEffect.LightingEnabled = false;

			m_plane = ToDispose(GeometricPrimitive.Plane.New(this.GraphicsDevice, 2, 2, 1, true));

			m_buffer = ToDispose(Buffer<VertexPositionColor>.Vertex.New(this.GraphicsDevice, m_vertices));
			m_layout = VertexInputLayout.FromBuffer(0, m_buffer);
		}

		public override void Update()
		{
		}

		public override void Draw(Camera camera)
		{
			DrawPlane(camera);
			DrawAxes(camera);
		}

		void DrawAxes(Camera camera)
		{
			var device = this.GraphicsDevice;

			var view = new Matrix()
			{
				Column1 = new Vector4(camera.Right, 0),
				Column2 = new Vector4(camera.Up, 0),
				Column3 = new Vector4(camera.Look, 1),
				Column4 = new Vector4(0, 0, 0, 1),
			};

			m_basicEffect.World = view;
			m_basicEffect.View = Matrix.Identity;
			m_basicEffect.Projection = Matrix.OrthoLH(2, 2, 0, 2);

			m_basicEffect.TextureEnabled = false;
			m_basicEffect.VertexColorEnabled = true;

			m_basicEffect.Alpha = 1;

			m_basicEffect.CurrentTechnique.Passes[0].Apply();

			device.SetDepthStencilState(device.DepthStencilStates.None);
			device.SetRasterizerState(device.RasterizerStates.CullNone);

			device.SetVertexBuffer(m_buffer);
			device.SetVertexInputLayout(m_layout);
			device.Draw(PrimitiveType.LineList, 6);

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetDepthStencilState(device.DepthStencilStates.Default);
		}

		void DrawPlane(Camera camera)
		{
			var device = this.GraphicsDevice;

			m_basicEffect.World = Matrix.Identity;
			m_basicEffect.View = Matrix.Identity;
			m_basicEffect.Projection = Matrix.Identity;

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
