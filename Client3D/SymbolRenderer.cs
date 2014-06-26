using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Client3D
{
	class SymbolRenderer : GameSystem
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct Vertex
		{
			[VertexElement("POSITION")]
			public Vector3 Position;
			[VertexElement("COLOR")]
			public Color Color;
			[VertexElement("TEXIDX")]
			public uint TexIdx;

			public Vertex(Vector3 pos, Color color, uint texIdx)
			{
				this.Position = pos;
				this.Color = color;
				this.TexIdx = texIdx;
			}
		}

		static readonly VertexInputLayout s_layout = VertexInputLayout.New<Vertex>(0);

		SymbolEffect m_effect;

		Vertex[] m_vertices;

		Buffer<Vertex> m_vertexBuffer;

		public SymbolRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			var sl = new Vector3(17, 11, 16);

			m_vertices = new Vertex[] {
				new Vertex(sl, Color.Red, (int)Dwarrowdelf.Client.SymbolID.Player),
				new Vertex(sl + new Vector3(-1, 0, 0), Color.Green, (int)Dwarrowdelf.Client.SymbolID.Wolf),
			};

			for (int i = 0; i < m_vertices.Length; ++i)
				m_vertices[i].Position += new Vector3(0.5f);

			game.GameSystems.Add(this);
		}

		public override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			m_effect = this.Content.Load<SymbolEffect>("SymbolEffect");

			m_effect.SymbolTextures = this.Content.Load<Texture2D>("TileSetTextureArray");

			m_vertexBuffer = Buffer.Vertex.New<Vertex>(this.GraphicsDevice, m_vertices);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			var cameraService = this.Services.GetService<ICameraService>();

			m_effect.EyePos = cameraService.Position;
			Matrix world = Matrix.Identity;
			m_effect.WorldViewProjection = world * cameraService.View * cameraService.Projection;
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			var device = this.GraphicsDevice;

			device.SetBlendState(device.BlendStates.AlphaBlend);
			device.SetVertexBuffer(m_vertexBuffer);
			device.SetVertexInputLayout(s_layout);

			device.Draw(PrimitiveType.PointList, m_vertices.Length);
		}
	}
}
