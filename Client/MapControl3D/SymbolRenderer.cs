using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class SymbolRenderer : GameComponent
	{
		SymbolEffect m_effect;

		int m_vertexCount;
		Buffer<SceneryVertex> m_vertexBuffer;

		bool m_invalid;

		ViewGridProvider m_viewGridProvider;

		MyGame m_game;

		public SymbolRenderer(MyGame game, ViewGridProvider viewGridProvider)
			: base(game)
		{
			m_game = game;
			m_viewGridProvider = viewGridProvider;

			m_invalid = true;

			//MovableObject3D.MovableMoved += MovableObject_MovableMoved;

			viewGridProvider.ViewGridCornerChanged +=
				(oldValue, newValue) => m_invalid = true;

			LoadContent();
		}

		void LoadContent()
		{
			m_effect = this.Content.Load<SymbolEffect>("SymbolEffect");

			m_effect.SymbolTextures = this.Content.Load<Texture2D>("TileSetTextureArray");
		}

		void MovableObject_MovableMoved(MovableObject obj)
		{
			m_invalid = true;
		}

		public override void Update()
		{
		}

		Color ToColor(GameColor color)
		{
			var rgb = color.ToGameColorRGB();
			return new Color(rgb.R, rgb.G, rgb.B);
		}

		VertexList<SceneryVertex> m_vertexList;

		void UpdateVertexBuffer()
		{
			if (m_game.Environment == null)
				return;

			var envContents = m_game.Environment.Contents;

			if (m_vertexList != null && envContents.Count > m_vertexList.Count)
				m_vertexList = null;

			if (m_vertexList == null)
				m_vertexList = new VertexList<SceneryVertex>(envContents.Count * 2);

			IntGrid3 viewGrid = m_viewGridProvider.ViewGrid;

			m_vertexList.Clear();

			foreach (var ob in envContents.OfType<ConcreteObject>())
			{
				if (viewGrid.Contains(ob.Location) == false)
					continue;

				var c = ob.Color;
				if (c == GameColor.None)
					c = ob.Material.Color;

				m_vertexList.Add(new SceneryVertex(ob.Location.ToVector3(), ToColor(c), (uint)ob.SymbolID));
			}

			if (m_vertexList.Count > 0)
			{
				if (m_vertexBuffer == null || m_vertexBuffer.ElementCount < m_vertexList.Count)
				{
					RemoveAndDispose(ref m_vertexBuffer);
					m_vertexBuffer = ToDispose(SharpDX.Toolkit.Graphics.Buffer.Vertex.New<SceneryVertex>(this.GraphicsDevice, m_vertexList.Count));
				}

				m_vertexBuffer.SetData(m_vertexList.Data, 0, m_vertexList.Count);
			}

			m_vertexCount = m_vertexList.Count;
		}

		public override void Draw(Camera camera)
		{
			m_invalid = true;
			if (m_invalid)
			{
				UpdateVertexBuffer();
				m_invalid = false;
			}

			if (m_vertexCount == 0)
				return;

			var device = this.GraphicsDevice;

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

			device.SetRasterizerState(device.RasterizerStates.CullNone);
			device.SetBlendState(device.BlendStates.AlphaBlend);
			//device.SetDepthStencilState(device.DepthStencilStates.None);

			device.SetVertexBuffer(m_vertexBuffer);
			device.Draw(PrimitiveType.PointList, m_vertexCount);

			device.SetRasterizerState(device.RasterizerStates.Default);
			device.SetBlendState(device.BlendStates.Default);
			//device.SetDepthStencilState(device.DepthStencilStates.Default);
		}
	}
}
