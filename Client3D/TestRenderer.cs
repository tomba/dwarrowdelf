using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Client3D
{

	// XXX sharpdx has its own
	class DirectionalLight
	{
		public Vector4 AmbientColor;
		public Vector4 DiffuseColor;
		public Vector4 SpecularColor;
		public Vector3 LightDirection;
	}

	class TestRenderer : GameSystem
	{
		public Effect Effect { get { return m_effect; } }
		Effect m_effect;

		ChunkManager m_chunkManager;

		GameMap m_map;
		public GameMap Map { get { return m_map; } }

		public bool DisableVSync { get; set; }
		public bool IsRotationEnabled { get; set; }
		public bool ShowBorders { get; set; }
		public int VerticesRendered { get; private set; }
		public int ChunkRecalcs { get; private set; }

		public TestRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			m_map = new GameMap();
			m_viewCorner1 = new IntPoint3(0, 0, 0);
			m_viewCorner2 = new IntPoint3(m_map.Size.Width - 1, m_map.Size.Height - 1, m_map.Size.Depth - 1);

			this.DirectionalLight = new DirectionalLight()
			{
				AmbientColor = new Vector4(new Vector3(0.4f), 1.0f),
				DiffuseColor = new Vector4(new Vector3(0.6f), 1.0f),
				SpecularColor = new Vector4(new Vector3(0.6f), 1.0f),
				LightDirection = Vector3.Normalize(new Vector3(1, 1, -4)),
			};

			m_chunkManager = ToDispose(new ChunkManager(this));

			game.GameSystems.Add(this);
		}

		Texture2D m_textures;

		protected override void LoadContent()
		{
			base.LoadContent();

			var device = this.Game.GraphicsDevice;

			m_effect = this.Content.Load<Effect>("TestEffect");

			string[] texFiles = System.IO.Directory.GetFiles("Content/BlockTextures");
			int numTextures = texFiles.Length;

			var format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			int w = 1024;
			int h = 1024;
			int mipLevels = 8;

			// XXX need RenderTarget to be able to generate mipmaps
			m_textures = Texture2D.New(device, w, h, mipLevels, format, arraySize: numTextures,
				flags: TextureFlags.RenderTarget | TextureFlags.ShaderResource);

			for (int texNum = 0; texNum < numTextures; ++texNum)
			{
				var texName = System.IO.Path.Combine("BlockTextures", System.IO.Path.GetFileNameWithoutExtension(texFiles[texNum]));

				using (var tex = this.Content.Load<Texture2D>(texName))
				{
					var data = tex.GetData<Color>();
					m_textures.SetData(data, texNum, 0);
				}
			}

			m_textures.GenerateMipMaps();

			m_effect.Parameters["blockTextures"].SetResource(m_textures);

			m_effect.Parameters["blockSampler"].SetResource(device.SamplerStates.LinearClamp);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		public DirectionalLight DirectionalLight { get; private set; }

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			m_effect.Parameters["ambientColor"].SetValue(this.DirectionalLight.AmbientColor);
			m_effect.Parameters["diffuseColor"].SetValue(this.DirectionalLight.DiffuseColor);
			m_effect.Parameters["specularColor"].SetValue(this.DirectionalLight.SpecularColor);
			m_effect.Parameters["lightDirection"].SetValue(this.DirectionalLight.LightDirection);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			m_chunkManager.Render();

			this.VerticesRendered = m_chunkManager.VerticesRendered;
			if (m_chunkManager.ChunkRecalcs > 0)
				this.ChunkRecalcs = m_chunkManager.ChunkRecalcs;
		}

		IntPoint3 m_viewCorner1;
		public IntPoint3 ViewCorner1
		{
			get { return m_viewCorner1; }

			set
			{
				if (value == m_viewCorner1)
					return;

				if (m_map.Size.Contains(value) == false)
					return;

				if (value.X > m_viewCorner2.X || value.Y > m_viewCorner2.Y || value.Z > m_viewCorner2.Z)
					return;

				var old = m_viewCorner1;
				m_viewCorner1 = value;

				var diff = value - old;

				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(old.Z);
					m_chunkManager.InvalidateChunksZ(value.Z);
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}
			}
		}

		IntPoint3 m_viewCorner2;
		public IntPoint3 ViewCorner2
		{
			get { return m_viewCorner2; }

			set
			{
				if (value == m_viewCorner2)
					return;

				if (m_map.Size.Contains(value) == false)
					return;

				if (value.X < m_viewCorner1.X || value.Y < m_viewCorner1.Y || value.Z < m_viewCorner1.Z)
					return;

				var old = m_viewCorner2;
				m_viewCorner2 = value;

				var diff = value - old;

				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(old.Z);
					m_chunkManager.InvalidateChunksZ(value.Z);
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}
			}
		}
	}
}
