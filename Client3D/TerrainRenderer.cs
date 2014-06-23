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
		public Effect Effect { get { return m_effect; } }
		Effect m_effect;

		ChunkManager m_chunkManager;

		public VoxelMap Map { get; private set; }

		public bool DisableVSync { get; set; }
		public bool IsRotationEnabled { get; set; }
		public bool ShowBorders { get; set; }
		public int VerticesRendered { get { return m_chunkManager.VerticesRendered; } }
		public int ChunksRendered { get { return m_chunkManager.ChunksRendered; } }
		public int ChunkRecalcs { get { return m_chunkManager.ChunkRecalcs; } }

		class DirectionalLight
		{
			public Vector4 AmbientColor;
			public Vector4 DiffuseColor;
			public Vector4 SpecularColor;
			public Vector3 LightDirection;
		}

		DirectionalLight m_directionalLight;

		public TerrainRenderer(Game game)
			: base(game)
		{
			this.Visible = true;
			this.Enabled = true;

			this.Map = VoxelMap.CreateFromTileData(new GameMap().Grid);
			//this.Map = VoxelMap.CreateBallMap(32, 16);
			//this.Map = VoxelMap.CreateSimplexMap(64, 0.2f);

			this.Map.UndefineHiddenVoxels();

			m_viewCorner1 = new IntPoint3(0, 0, 0);
			m_viewCorner2 = new IntPoint3(this.Map.Width - 1, this.Map.Height - 1, this.Map.Depth - 1);

			m_directionalLight = new DirectionalLight()
			{
				AmbientColor = new Vector4(new Vector3(0.4f), 1.0f),
				DiffuseColor = new Vector4(new Vector3(0.6f), 1.0f),
				SpecularColor = new Vector4(new Vector3(0.6f), 1.0f),
				LightDirection = Vector3.Normalize(new Vector3(1, 2, -4)),
			};

			m_chunkManager = ToDispose(new ChunkManager(this));

			game.GameSystems.Add(this);
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			m_effect = this.Content.Load<Effect>("TerrainEffect");

			var textures = this.Content.Load<Texture2D>("TileTextureArray");
			m_effect.Parameters["blockTextures"].SetResource(textures);

			m_effect.Parameters["blockSampler"].SetResource(this.GraphicsDevice.SamplerStates.LinearClamp);
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

			m_effect.Parameters["ambientColor"].SetValue(m_directionalLight.AmbientColor);
			m_effect.Parameters["diffuseColor"].SetValue(m_directionalLight.DiffuseColor);
			m_effect.Parameters["specularColor"].SetValue(m_directionalLight.SpecularColor);
			m_effect.Parameters["lightDirection"].SetValue(m_directionalLight.LightDirection);

			var renderPass = m_effect.CurrentTechnique.Passes[0];
			renderPass.Apply();

			m_chunkManager.Draw(gameTime);
		}

		IntPoint3 m_viewCorner1;
		public IntPoint3 ViewCorner1
		{
			get { return m_viewCorner1; }

			set
			{
				if (value == m_viewCorner1)
					return;

				if (this.Map.Size.Contains(value) == false)
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

				if (this.Map.Size.Contains(value) == false)
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


		public IntPoint2? ClickPos;

		void HandleMouseClick()
		{
			if (this.ClickPos == null)
				return;

			var buf = this.GraphicsDevice.DepthStencilBuffer;
			var p = this.ClickPos.Value;

			// XXX this copies the whole buffer
			var arr = buf.GetData<uint>();
			uint v = arr[p.Y * buf.Width + p.X];

			if (buf.DepthFormat != DepthFormat.Depth24Stencil8)
				throw new Exception();

			// 24 bit depth

			float d = (float)(v & 0xffffff);
			d /= 0xffffff;

			var wp = ScreenToWorld(p, d);

			Console.WriteLine("{0} -> ({1}, {2}, {3})", new Vector3(p.X, p.Y, d),
				Math.Floor(wp.X), Math.Floor(wp.Y), Math.Floor(wp.Z));

			/*
			using (var img = this.GraphicsDevice.DepthStencilBuffer.GetDataAsImage())
			{
				var pixbuf = img.GetPixelBuffer(0, 0);
				var pix = pixbuf.GetPixel<uint>(this.ClickPos.Value.X, this.ClickPos.Value.Y);
				float p = (float)(pix >> 8);
				Console.WriteLine("GOT {0:X}, {1}", pix, p);
			}
			*/
			/*
				context.CopyResource(m_depthBuffer, m_stagingBuffer);

				var box = context.MapSubresource(m_stagingBuffer, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

				float f = Utilities.Read<float>(box.DataPointer + p.X * 4 + p.Y * box.RowPitch);
				var wp = ScreenToWorld(p, f);
				Debug.Print("{0} -> ({1}, {2}, {3})", new Vector3(p.X, p.Y, f), Math.Floor(wp.X), Math.Floor(wp.Y), Math.Floor(wp.Z));

				context.UnmapSubresource(m_stagingBuffer, 0);
			*/

			this.ClickPos = null;
		}

		Vector3 ScreenToWorld(IntPoint2 sp, float d)
		{
			if (d == 1.0f)
				return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

			float w = this.Game.Window.ClientBounds.Width;
			float h = this.Game.Window.ClientBounds.Height;

			var buf = this.GraphicsDevice.DepthStencilBuffer;
			if (buf.Width != w || buf.Height != h)
				throw new Exception();

			float px = (sp.X - 0.5f * w) / w * 2;
			float py = (h - 0.5f * h - sp.Y) / h * 2;

			var cameraService = this.Services.GetService<ICameraService>();

			var projInverse = Matrix.Invert(cameraService.Projection);
			var viewInverse = Matrix.Invert(cameraService.View);

			var pp = new Vector3(px, py, d);

			var p1 = Vector3.TransformCoordinate(pp, projInverse);
			var p2 = Vector3.TransformCoordinate(p1, viewInverse);

			return p2;
		}
	}
}
