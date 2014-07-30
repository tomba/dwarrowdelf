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
	class TerrainRenderer : GameSystem
	{
		public TerrainEffect Effect { get { return m_effect; } }
		TerrainEffect m_effect;

		ChunkManager m_chunkManager;

		public bool DisableVSync { get; set; }
		public bool IsRotationEnabled { get; set; }
		public bool ShowBorders { get; set; }
		public int VerticesRendered { get { return m_chunkManager.VerticesRendered; } }
		public int ChunksRendered { get { return m_chunkManager.ChunksRendered; } }
		public int ChunkRecalcs { get { return m_chunkManager.ChunkRecalcs; } }

		DirectionalLight m_directionalLight;

		public TerrainRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			var map = GlobalData.VoxelMap;

			m_viewCorner1 = new IntVector3(0, 0, 0);
			m_viewCorner2 = new IntVector3(map.Width - 1, map.Height - 1, map.Depth - 1);

			m_directionalLight = new DirectionalLight()
			{
				AmbientColor = new Vector3(0.4f),
				DiffuseColor = new Vector3(0.6f),
				SpecularColor = new Vector3(0.6f),
				LightDirection = Vector3.Normalize(new Vector3(1, 2, -4)),
			};

			m_chunkManager = new ChunkManager(this);

			game.GameSystems.Add(this);
		}

		public override void Initialize()
		{
			base.Initialize();

			m_chunkManager.Initialize();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			m_effect = this.Content.Load<TerrainEffect>("TerrainEffect");

			m_effect.TileTextures = this.Content.Load<Texture2D>("TileSetTextureArray");
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			var tTime = (float)gameTime.TotalGameTime.TotalSeconds;

			if (IsRotationEnabled)
			{
				Matrix m = Matrix.Identity;
				m *= Matrix.RotationX(tTime);
				m *= Matrix.RotationY(tTime * 1.1f);
				m *= Matrix.RotationZ(tTime * 0.7f);
				m_directionalLight.LightDirection = Vector3.TransformNormal(Vector3.Normalize(new Vector3(1, 1, 1)), m);
			}

			HandleMouseClick();

			m_chunkManager.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			m_effect.SetDirectionalLight(m_directionalLight);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			m_chunkManager.Draw(gameTime);
		}

		IntVector3 m_viewCorner1;
		public IntVector3 ViewCorner1
		{
			get { return m_viewCorner1; }

			set
			{
				if (value == m_viewCorner1)
					return;

				if (GlobalData.VoxelMap.Size.Contains(value) == false)
					return;

				if (value.X > m_viewCorner2.X || value.Y > m_viewCorner2.Y || value.Z > m_viewCorner2.Z)
					return;

				var old = m_viewCorner1;
				m_viewCorner1 = value;

				var diff = value - old;

				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(Math.Min(old.Z, value.Z), Math.Max(old.Z, value.Z));
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}
			}
		}

		IntVector3 m_viewCorner2;
		public IntVector3 ViewCorner2
		{
			get { return m_viewCorner2; }

			set
			{
				if (value == m_viewCorner2)
					return;

				if (GlobalData.VoxelMap.Size.Contains(value) == false)
					return;

				if (value.X < m_viewCorner1.X || value.Y < m_viewCorner1.Y || value.Z < m_viewCorner1.Z)
					return;

				var old = m_viewCorner2;
				m_viewCorner2 = value;

				var diff = value - old;

				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(Math.Min(old.Z, value.Z), Math.Max(old.Z, value.Z));
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}
			}
		}


		public IntVector2? ClickPos;

		void HandleMouseClick()
		{
			if (this.ClickPos == null)
				return;

			HandlePickWithRay();

			this.ClickPos = null;
		}

		void HandlePickWithRay()
		{
			var p = this.ClickPos.Value;

			var camera = this.Services.GetService<ICameraService>();

			var wvp = camera.View * camera.Projection;

			var ray = Ray.GetPickRay(p.X, p.Y, this.GraphicsDevice.Viewport, wvp);

			VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
				(x, y, z, vx, dir) =>
				{
					if (vx.IsEmpty)
						return false;

					var l = new IntVector3(x, y, z);

					Console.WriteLine("pick: {0} face: {1}", l, dir);

					return true;
				});
		}
	}
}
