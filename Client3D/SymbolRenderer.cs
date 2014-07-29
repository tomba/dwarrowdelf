using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

		Vertex[] m_sceneryVertices;

		Buffer<Vertex> m_sceneryVertexBuffer;

		Vertex[] m_vertices;

		Buffer<Vertex> m_vertexBuffer;

		public SymbolRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

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

			{
				var sl = new Vector3(17, 11, 16);

				List<Vertex> vertices = new List<Vertex>();

				vertices.Add(new Vertex(sl, Color.Red, (int)Dwarrowdelf.Client.SymbolID.Player));
				vertices.Add(new Vertex(sl + new Vector3(-1, 0, 0), Color.Green, (int)Dwarrowdelf.Client.SymbolID.Wolf));

				m_vertices = vertices.ToArray();

				for (int i = 0; i < m_vertices.Length; ++i)
					m_vertices[i].Position += new Vector3(0.5f);

				if (m_vertices.Length > 0)
					m_vertexBuffer = Buffer.Vertex.New<Vertex>(this.GraphicsDevice, m_vertices);
			}

			{
				List<Vertex> vertices = new List<Vertex>();

				var grid = GlobalData.VoxelMap.Grid;
				int width = GlobalData.VoxelMap.Width;
				int height = GlobalData.VoxelMap.Height;
				int depth = GlobalData.VoxelMap.Depth;

				Parallel.For(0, depth, z =>
				{
					for (int y = 0; y < height; ++y)
						for (int x = 0; x < width; ++x)
						{
							if ((grid[z, y, x].Flags & VoxelFlags.Tree) != 0)
							{
								lock (vertices)
									vertices.Add(new Vertex(new Vector3(x, y, z), Color.LightGreen,
										(int)Dwarrowdelf.Client.SymbolID.ConiferousTree));
							}
						}
				});

				m_sceneryVertices = vertices.ToArray();

				for (int i = 0; i < m_sceneryVertices.Length; ++i)
					m_sceneryVertices[i].Position += new Vector3(0.5f);

				if (m_sceneryVertices.Length > 0)
					m_sceneryVertexBuffer = Buffer.Vertex.New<Vertex>(this.GraphicsDevice, m_sceneryVertices);
			}
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
			var device = this.GraphicsDevice;

			base.Draw(gameTime);

			var cameraService = this.Services.GetService<ICameraService>();

			device.SetRasterizerState(device.RasterizerStates.CullNone);
			device.SetVertexInputLayout(s_layout);

			if (m_sceneryVertexBuffer != null)
			{
				device.SetBlendState(device.BlendStates.Default);

				m_effect.CurrentTechnique = m_effect.Techniques["ModeCross"];
				m_effect.CurrentTechnique.Passes[0].Apply();

				device.SetVertexBuffer(m_sceneryVertexBuffer);
				device.Draw(PrimitiveType.PointList, m_sceneryVertices.Length);
			}

			if (m_vertexBuffer != null)
			{
				var angle = (float)System.Math.Acos(Vector3.Dot(-Vector3.UnitZ, cameraService.Look));
				angle = MathUtil.RadiansToDegrees(angle);
				if (System.Math.Abs(angle) < 45)
					m_effect.CurrentTechnique = m_effect.Techniques["ModeFlat"];
				else
					m_effect.CurrentTechnique = m_effect.Techniques["ModeFollow"];

				m_effect.CurrentTechnique.Passes[0].Apply();

				device.SetBlendState(device.BlendStates.AlphaBlend);
				//device.SetDepthStencilState(device.DepthStencilStates.None);
				device.SetVertexBuffer(m_vertexBuffer);
				device.Draw(PrimitiveType.PointList, m_vertices.Length);
			}
		}
	}
}
