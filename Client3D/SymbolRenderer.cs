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
		SymbolEffect m_effect;

		SceneryVertex[] m_vertices;

		Buffer<SceneryVertex> m_vertexBuffer;

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

				List<SceneryVertex> vertices = new List<SceneryVertex>();

				vertices.Add(new SceneryVertex(sl, Color.Red, (int)Dwarrowdelf.Client.SymbolID.Player));
				vertices.Add(new SceneryVertex(sl + new Vector3(-1, 0, 0), Color.Green, (int)Dwarrowdelf.Client.SymbolID.Wolf));

				m_vertices = vertices.ToArray();

				if (m_vertices.Length > 0)
					m_vertexBuffer = Buffer.Vertex.New<SceneryVertex>(this.GraphicsDevice, m_vertices);
			}
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			var device = this.GraphicsDevice;

			if (m_vertexBuffer == null)
				return;

			var camera = this.Services.GetService<ICameraService>();

			m_effect.EyePos = camera.Position;
			m_effect.ViewProjection = camera.View * camera.Projection;

			var angle = (float)System.Math.Acos(Vector3.Dot(-Vector3.UnitZ, camera.Look));
			angle = MathUtil.RadiansToDegrees(angle);
			if (System.Math.Abs(angle) < 45)
				m_effect.CurrentTechnique = m_effect.Techniques["ModeFlat"];
			else
				m_effect.CurrentTechnique = m_effect.Techniques["ModeFollow"];

			m_effect.CurrentTechnique.Passes[0].Apply();

			var offset = new IntVector3();
			m_effect.SetPerObjectConstBuf(offset);

			device.SetBlendState(device.BlendStates.AlphaBlend);
			//device.SetDepthStencilState(device.DepthStencilStates.None);
			device.SetVertexBuffer(m_vertexBuffer);
			device.Draw(PrimitiveType.PointList, m_vertices.Length);
		}
	}
}
