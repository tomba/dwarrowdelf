using Dwarrowdelf;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class ChunkManager : Component
	{
		Chunk[] m_chunks;

		TestRenderer m_scene;

		public int VerticesRendered { get; private set; }
		public int ChunkRecalcs { get; private set; }

		public ChunkManager(TestRenderer scene)
		{
			m_scene = scene;

			CreateChunks();
		}

		void CreateChunks()
		{
			var map = m_scene.Map;

			int xChunks = map.Size.Width / Chunk.CHUNK_SIZE;
			int yChunks = map.Size.Height / Chunk.CHUNK_SIZE;
			int zChunks = map.Size.Depth / Chunk.CHUNK_SIZE;

			m_chunks = new Chunk[xChunks * yChunks * zChunks];

			// Organize chunks from up to down to avoid overdraw
			int idx = 0;
			for (int z = zChunks - 1; z >= 0; --z)
			{
				for (int y = 0; y < yChunks; ++y)
				{
					for (int x = 0; x < xChunks; ++x)
					{
						var chunkOffset = new IntVector3(x, y, z) * Chunk.CHUNK_SIZE;
						var chunk = ToDispose(new Chunk(map, chunkOffset));
						m_chunks[idx++] = chunk;
					}
				}
			}
		}

		public void InvalidateChunks()
		{
			foreach (var chunk in m_chunks)
				chunk.InvalidateChunk();
		}

		public void InvalidateChunksZ(int z)
		{
			foreach (var chunk in m_chunks)
			{
				if (z > chunk.ChunkOffset.Z - 1 && z < chunk.ChunkOffset.Z + Chunk.CHUNK_SIZE + 1)
					chunk.InvalidateChunk();
			}
		}

		public void Render()
		{
			this.VerticesRendered = 0;
			this.ChunkRecalcs = 0;

			var cameraService = m_scene.Services.GetService<ICameraService>();

			// Vertex Shader

			var viewProjMatrix = Matrix.Transpose(cameraService.View * cameraService.Projection);
			viewProjMatrix.Transpose();
			m_scene.Effect.Parameters["g_viewProjMatrix"].SetValue(ref viewProjMatrix);

			// Pixel Shader
			m_scene.Effect.Parameters["g_eyePos"].SetValue(cameraService.Position);


			var frustum = cameraService.Frustum;

			foreach (var chunk in m_chunks)
			{
				var res = frustum.Contains(ref chunk.BBox);

				if (res != ContainmentType.Disjoint)
				{
					RenderChunk(chunk);
				}
				else
				{
					chunk.Free();
				}
			}
		}

		void RenderChunk(Chunk chunk)
		{
			var worldMatrix = Matrix.Identity;
			worldMatrix.Transpose();

			m_scene.Effect.Parameters["worldMatrix"].SetValue(ref worldMatrix);

			chunk.Render(m_scene);

			this.VerticesRendered += chunk.VertexCount;
			this.ChunkRecalcs += chunk.ChunkRecalcs;
		}
	}
}
