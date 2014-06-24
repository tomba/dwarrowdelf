﻿//#define USE_NONPARALLEL

using Dwarrowdelf;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client3D
{
	class ChunkManager : Component
	{
		Chunk[] m_chunks;

		TerrainRenderer m_scene;

		public int VerticesRendered { get; private set; }
		public int ChunksRendered { get; private set; }
		public int ChunkRecalcs { get; private set; }

		/// <summary>
		/// Size in chunks
		/// </summary>
		public IntSize3 Size { get; private set; }

		Vector3 m_oldCameraPos;

		public ChunkManager(TerrainRenderer scene)
		{
			m_scene = scene;

			CreateChunks();

			m_oldCameraPos = m_scene.Services.GetService<ICameraService>().Position;
		}

		void CreateChunks()
		{
			var map = m_scene.Map;

			int xChunks = map.Size.Width / Chunk.CHUNK_SIZE;
			int yChunks = map.Size.Height / Chunk.CHUNK_SIZE;
			int zChunks = map.Size.Depth / Chunk.CHUNK_SIZE;

			this.Size = new IntSize3(xChunks, yChunks, zChunks);

			m_chunks = new Chunk[this.Size.Volume];

			// Organize chunks from up to down to avoid overdraw
			for (int z = zChunks - 1; z >= 0; --z)
			{
				for (int y = 0; y < yChunks; ++y)
				{
					for (int x = 0; x < xChunks; ++x)
					{
						var chunkPosition = new IntVector3(x, y, z);
						var chunk = ToDispose(new Chunk(map, chunkPosition));
						m_chunks[this.Size.GetIndex(x, y, z)] = chunk;
					}
				}
			}
		}

		public void InvalidateChunks()
		{
			foreach (var chunk in m_chunks)
				chunk.InvalidateChunk();
		}

		public void InvalidateChunk(IntPoint3 cp)
		{
			var chunk = m_chunks[this.Size.GetIndex(cp)];

			chunk.InvalidateChunk();
		}

		public void InvalidateChunksZ(int fromZ, int toZ)
		{
			int fromIdx = this.Size.GetIndex(0, 0, fromZ / Chunk.CHUNK_SIZE);
			int toIdx = this.Size.GetIndex(0, 0, (toZ / Chunk.CHUNK_SIZE) + 1);

			for (int idx = fromIdx; idx < toIdx; ++idx)
			{
				var chunk = m_chunks[idx];
				chunk.InvalidateChunk();
			}
		}

		void InvalidateYZPlane(int x)
		{
			//Console.WriteLine("invalidate x plane {0}", x);

			var rect = new IntGrid2(0, 0, this.Size.Height, this.Size.Depth);

			foreach (var _p in rect.Range())
			{
				var p = new IntPoint3(x, _p.X, _p.Y);
				InvalidateChunk(p);
			}
		}

		void InvalidateXZPlane(int y)
		{
			//Console.WriteLine("invalidate y plane {0}", y);

			var rect = new IntGrid2(0, 0, this.Size.Width, this.Size.Depth);

			foreach (var _p in rect.Range())
			{
				var p = new IntPoint3(_p.X, y, _p.Y);
				InvalidateChunk(p);
			}
		}

		void InvalidateXYPlane(int z)
		{
			//Console.WriteLine("invalidate z plane {0}", z);

			var rect = new IntGrid2(0, 0, this.Size.Width, this.Size.Height);

			foreach (var _p in rect.Range())
			{
				var p = new IntPoint3(_p.X, _p.Y, z);
				InvalidateChunk(p);
			}
		}

		void InvalidateDueVisibilityChange()
		{
			var cameraService = m_scene.Services.GetService<ICameraService>();

			var cameraPos = cameraService.Position;

			var p1 = m_oldCameraPos / Chunk.CHUNK_SIZE;
			var p2 = cameraPos / Chunk.CHUNK_SIZE;

			var minf = Vector3.Min(p1, p2);
			var maxf = Vector3.Max(p1, p2);

			var min = minf.ToFloorIntPoint3();
			var max = maxf.ToFloorIntPoint3();

			var diff = max - min;

			if (diff.X != 0)
			{
				for (int x = Math.Max(min.X, 0); x <= Math.Min(max.X, this.Size.Width - 1); ++x)
					InvalidateYZPlane(x);
			}

			if (diff.Y != 0)
			{
				for (int y = Math.Max(min.Y, 0); y <= Math.Min(max.Y, this.Size.Height - 1); ++y)
					InvalidateXZPlane(y);
			}

			if (diff.Z != 0)
			{
				for (int z = Math.Max(min.Z, 0); z <= Math.Min(max.Z, this.Size.Depth - 1); ++z)
					InvalidateXYPlane(z);
			}

			m_oldCameraPos = cameraPos;
		}

		public void Update(GameTime gameTime)
		{
			var cameraService = m_scene.Services.GetService<ICameraService>();

			InvalidateDueVisibilityChange();

			var frustum = cameraService.Frustum;

			int numVertices = 0;
			int numChunks = 0;
			int numChunkRecalcs = 0;

			var eyePos = cameraService.Position;

#if USE_NONPARALLEL
			foreach (var chunk in m_chunks)
#else
			Parallel.ForEach(m_chunks, chunk =>
#endif
			{
				var res = frustum.Contains(ref chunk.BBox);

				if (res == ContainmentType.Disjoint)
				{
					chunk.IsEnabled = false;

					chunk.Free();
				}
				else
				{
					chunk.IsEnabled = true;

					if (chunk.IsInvalid)
						Interlocked.Increment(ref numChunkRecalcs);

					chunk.Update(m_scene, eyePos);

					Interlocked.Add(ref numVertices, chunk.VertexCount);
					Interlocked.Increment(ref numChunks);
				}
#if USE_NONPARALLEL
			}
#else
			});
#endif
			this.VerticesRendered = numVertices;
			this.ChunksRendered = numChunks;
			this.ChunkRecalcs = numChunkRecalcs;
		}

		public void Draw(GameTime gameTime)
		{
			var cameraService = m_scene.Services.GetService<ICameraService>();

			// Vertex Shader

			var viewProjMatrix = Matrix.Transpose(cameraService.View * cameraService.Projection);
			viewProjMatrix.Transpose();
			m_scene.Effect.Parameters["g_viewProjMatrix"].SetValue(ref viewProjMatrix);

			// Pixel Shader
			m_scene.Effect.Parameters["g_eyePos"].SetValue(cameraService.Position);

			var perObCBuf = m_scene.Effect.ConstantBuffers["PerObjectBuffer"];

			foreach (var chunk in m_chunks)
			{
				if (chunk.IsEnabled == false)
					continue;

				var worldMatrix = Matrix.Translation(chunk.ChunkOffset.ToVector3());
				perObCBuf.Parameters["worldMatrix"].SetValue(ref worldMatrix);
				perObCBuf.Update();

				chunk.Render(m_scene);
			}
		}
	}
}
