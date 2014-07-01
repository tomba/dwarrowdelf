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

			m_chunkManager = ToDispose(new ChunkManager(this));

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

			VoxelRayCast(ray.Position, ray.Direction, camera.FarZ,
				(x, y, z, vx, dir) =>
				{
					if (vx.IsEmpty)
						return false;

					var l = new IntVector3(x, y, z);

					Console.WriteLine("pick: {0} face: {1}", l, dir);

					return true;
				});
		}

		/**
		 * Call the callback with (x,y,z,value,face) of all blocks along the line
		 * segment from point 'origin' in vector direction 'direction' of length
		 * 'radius'. 'radius' may be infinite.
		 * 
		 * 'face' is the normal vector of the face of that block that was entered.
		 * It should not be used after the callback returns.
		 * 
		 * If the callback returns a true value, the traversal will be stopped.
		 */

		delegate bool RayCastDelegate(int x, int y, int z, Voxel voxel, Direction dir);

		void VoxelRayCast(Vector3 origin, Vector3 direction, float radius, RayCastDelegate callback)
		{
			int wx = GlobalData.VoxelMap.Width;
			int wy = GlobalData.VoxelMap.Height;
			int wz = GlobalData.VoxelMap.Depth;
			var blocks = GlobalData.VoxelMap.Grid;

			// From "A Fast Voxel Traversal Algorithm for Ray Tracing"
			// by John Amanatides and Andrew Woo, 1987
			// <http://www.cse.yorku.ca/~amana/research/grid.pdf>
			// <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
			// Extensions to the described algorithm:
			//   • Imposed a distance limit.
			//   • The face passed through to reach the current cube is provided to
			//     the callback.

			// The foundation of this algorithm is a parameterized representation of
			// the provided ray,
			//                    origin + t * direction,
			// except that t is not actually stored; rather, at any given point in the
			// traversal, we keep track of the *greater* t values which we would have
			// if we took a step sufficient to cross a cube boundary along that axis
			// (i.e. change the integer part of the coordinate) in the variables
			// tMaxX, tMaxY, and tMaxZ.

			// Cube containing origin point.
			int x = MyMath.Floor(origin[0]);
			int y = MyMath.Floor(origin[1]);
			int z = MyMath.Floor(origin[2]);
			// Break out direction vector.
			float dx = direction.X;
			float dy = direction.Y;
			float dz = direction.Z;
			// Direction to increment x,y,z when stepping.
			int stepX = Math.Sign(dx);
			int stepY = Math.Sign(dy);
			int stepZ = Math.Sign(dz);
			// See description above. The initial values depend on the fractional
			// part of the origin.
			float tMaxX = intbound(origin.X, dx);
			float tMaxY = intbound(origin.Y, dy);
			float tMaxZ = intbound(origin.Z, dz);
			// The change in t when taking a step (always positive).
			float tDeltaX = stepX / dx;
			float tDeltaY = stepY / dy;
			float tDeltaZ = stepZ / dz;
			// Buffer for reporting faces to the callback.
			Direction face = Direction.None;

			// Avoids an infinite loop.
			if (dx == 0 && dy == 0 && dz == 0)
				throw new Exception("Raycast in zero direction!");

			// Rescale from units of 1 cube-edge to units of 'direction' so we can
			// compare with 't'.
			radius /= (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

			while (/* ray has not gone past bounds of world */
				   (stepX > 0 ? x < wx : x >= 0) &&
				   (stepY > 0 ? y < wy : y >= 0) &&
				   (stepZ > 0 ? z < wz : z >= 0))
			{

				// Invoke the callback, unless we are not *yet* within the bounds of the
				// world.
				if (!(x < 0 || y < 0 || z < 0 || x >= wx || y >= wy || z >= wz))
				{
					if (callback(x, y, z, blocks[z, y, x], face))
						break;
				}

				// tMaxX stores the t-value at which we cross a cube boundary along the
				// X axis, and similarly for Y and Z. Therefore, choosing the least tMax
				// chooses the closest cube boundary. Only the first case of the four
				// has been commented in detail.
				if (tMaxX < tMaxY)
				{
					if (tMaxX < tMaxZ)
					{
						if (tMaxX > radius)
							break;
						// Update which cube we are now in.
						x += stepX;
						// Adjust tMaxX to the next X-oriented boundary crossing.
						tMaxX += tDeltaX;
						// Record the normal vector of the cube face we entered.
						face = -stepX > 0 ? Direction.East : Direction.West;
					}
					else
					{
						if (tMaxZ > radius)
							break;
						z += stepZ;
						tMaxZ += tDeltaZ;
						face = -stepZ > 0 ? Direction.Up : Direction.Down;
					}
				}
				else
				{
					if (tMaxY < tMaxZ)
					{
						if (tMaxY > radius)
							break;
						y += stepY;
						tMaxY += tDeltaY;
						face = -stepY > 0 ? Direction.North : Direction.South;
					}
					else
					{
						// Identical to the second case, repeated for simplicity in
						// the conditionals.
						if (tMaxZ > radius)
							break;
						z += stepZ;
						tMaxZ += tDeltaZ;
						face = -stepZ > 0 ? Direction.Up : Direction.Down;
					}
				}
			}
		}

		// Find the smallest positive t such that s+t*ds is an integer.
		float intbound(float s, float ds)
		{
			if (ds < 0)
				return intbound(-s, -ds);

			s = mod(s, 1);
			// problem is now s+t*ds = 1
			return (1 - s) / ds;
		}

		float mod(float value, float modulus)
		{
			return (value % modulus + modulus) % modulus;
		}
	}
}
