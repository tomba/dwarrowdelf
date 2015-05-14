using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dwarrowdelf.Client
{
	class TerrainRenderer : GameComponent
	{
		public TerrainEffect Effect { get { return m_effect; } }
		TerrainEffect m_effect;

		public SymbolEffect SymbolEffect { get { return m_symbolEffect; } }
		SymbolEffect m_symbolEffect;

		public ChunkManager ChunkManager { get { return m_chunkManager; } }
		ChunkManager m_chunkManager;

		public bool IsRotationEnabled { get; set; }
		public bool ShowBorders { get; set; }
		public int VerticesRendered { get { return m_chunkManager.VerticesRendered; } }
		public int ChunkRecalcs { get { return m_chunkManager.ChunkRecalcs; } set { m_chunkManager.ChunkRecalcs = value; } }

		DirectionalLight m_directionalLight;

		public TerrainRenderer(MyGame game, Camera camera, ViewGridProvider viewGridProvider)
			: base(game)
		{
			m_directionalLight = new DirectionalLight()
			{
				AmbientColor = new Vector3(0.4f),
				DiffuseColor = new Vector3(0.6f),
				SpecularColor = new Vector3(0.1f),
				LightDirection = Vector3.Normalize(new Vector3(1, 2, -4)),
			};

			m_chunkManager = ToDispose(new ChunkManager(this, camera, viewGridProvider));

			LoadContent();
		}

		void LoadContent()
		{
			m_effect = this.Content.Load<TerrainEffect>("TerrainEffect");

			m_effect.TileTextures = this.Content.Load<Texture2D>("TileSetTextureArray");

			m_symbolEffect = this.Content.Load<SymbolEffect>("SymbolEffect");

			m_symbolEffect.SymbolTextures = this.Content.Load<Texture2D>("TileSetTextureArray");
		}

		public override void Update()
		{
			var tTime = (float)this.Game.Time.TotalTime.TotalSeconds;

			if (IsRotationEnabled)
			{
				Matrix m = Matrix.Identity;
				m *= Matrix.RotationX(tTime);
				m *= Matrix.RotationY(tTime * 1.1f);
				m *= Matrix.RotationZ(tTime * 0.7f);
				m_directionalLight.LightDirection = Vector3.TransformNormal(Vector3.Normalize(new Vector3(1, 1, 1)), m);
			}
		}

		public override void Draw(Camera camera)
		{
			m_chunkManager.PrepareDraw();

			var device = this.GraphicsDevice;

			device.SetRasterizerState(((MyGame)this.Game).RasterizerState);

			// voxels
			{
				m_effect.EyePos = camera.Position;
				m_effect.ViewProjection = camera.View * camera.Projection;

				m_effect.SetDirectionalLight(m_directionalLight);

				var renderPass = m_effect.CurrentTechnique.Passes[0];
				renderPass.Apply();

				m_chunkManager.Draw(camera);
			}

			device.SetRasterizerState(device.RasterizerStates.Default);

			// trees
			{
				device.SetRasterizerState(device.RasterizerStates.CullNone);
				device.SetBlendState(device.BlendStates.AlphaBlend);

				m_symbolEffect.EyePos = camera.Position;
				m_symbolEffect.ScreenUp = camera.ScreenUp;
				m_symbolEffect.ViewProjection = camera.View * camera.Projection;

				var angle = (float)System.Math.Acos(Vector3.Dot(-Vector3.UnitZ, camera.Look));
				angle = MathUtil.RadiansToDegrees(angle);
				if (System.Math.Abs(angle) < 45)
					m_symbolEffect.CurrentTechnique = m_symbolEffect.Techniques["ModeFlat"];
				else
					m_symbolEffect.CurrentTechnique = m_symbolEffect.Techniques["ModeCross"];

				var renderPass = m_symbolEffect.CurrentTechnique.Passes[0];
				renderPass.Apply();

				m_chunkManager.DrawTrees();

				device.SetBlendState(device.BlendStates.Default);
				device.SetRasterizerState(device.RasterizerStates.Default);
			}
		}
	}
}
