using Dwarrowdelf;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client3D
{
	class ChunkManager : Component
	{
		class VertexListCacheItem
		{
			public Chunk Chunk;
			public VertexList<TerrainVertex> TerrainVertexList;
			public VertexList<SceneryVertex> SceneryVertexList;

			public VertexListCacheItem()
			{
				this.TerrainVertexList = new VertexList<TerrainVertex>(Chunk.MAX_VERTICES_PER_CHUNK);
				this.SceneryVertexList = new VertexList<SceneryVertex>(Chunk.VOXELS_PER_CHUNK);
			}
		}

		Chunk[] m_chunks;

		TerrainRenderer m_scene;

		public int VerticesRendered { get; private set; }
		public int ChunksRendered { get; private set; }
		public int ChunkRecalcs { get { return m_chunkRecalcs; } set { m_chunkRecalcs = value; } }
		int m_chunkRecalcs;

		/// <summary>
		/// Size in chunks
		/// </summary>
		public IntSize3 Size { get; private set; }

		ICameraService m_camera;

		Vector3 m_oldCameraPos;

		public ChunkManager(TerrainRenderer scene)
		{
			m_scene = scene;

			CreateChunks();
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);

			foreach (var chunk in m_chunks)
			{
				if (chunk.IsEnabled)
					chunk.Free();
			}
		}

		void CreateChunks()
		{
			var map = GlobalData.VoxelMap;

			int xChunks = map.Size.Width / Chunk.CHUNK_SIZE;
			int yChunks = map.Size.Height / Chunk.CHUNK_SIZE;
			int zChunks = map.Size.Depth / Chunk.CHUNK_SIZE;

			this.Size = new IntSize3(xChunks, yChunks, zChunks);

			m_chunks = new Chunk[this.Size.Volume];

			for (int z = 0; z < zChunks; ++z)
			{
				for (int y = 0; y < yChunks; ++y)
				{
					for (int x = 0; x < xChunks; ++x)
					{
						var chunkPosition = new IntVector3(x, y, z);
						var chunk = new Chunk(map, chunkPosition);
						m_chunks[this.Size.GetIndex(x, y, z)] = chunk;
					}
				}
			}
		}

		public void Initialize()
		{
			m_camera = m_scene.Services.GetService<ICameraService>();

			m_oldCameraPos = m_camera.Position;
		}

		public void InvalidateChunks()
		{
			foreach (var chunk in m_chunks)
				chunk.InvalidateChunk();
		}

		void InvalidateChunk(IntVector3 cp)
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
				var p = new IntVector3(x, _p.X, _p.Y);
				InvalidateChunk(p);
			}
		}

		void InvalidateXZPlane(int y)
		{
			//Console.WriteLine("invalidate y plane {0}", y);

			var rect = new IntGrid2(0, 0, this.Size.Width, this.Size.Depth);

			foreach (var _p in rect.Range())
			{
				var p = new IntVector3(_p.X, y, _p.Y);
				InvalidateChunk(p);
			}
		}

		void InvalidateXYPlane(int z)
		{
			//Console.WriteLine("invalidate z plane {0}", z);

			var rect = new IntGrid2(0, 0, this.Size.Width, this.Size.Height);

			foreach (var _p in rect.Range())
			{
				var p = new IntVector3(_p.X, _p.Y, z);
				InvalidateChunk(p);
			}
		}

		void InvalidateDueVisibilityChange()
		{
			var cameraPos = m_camera.Position;

			var p1 = m_oldCameraPos / Chunk.CHUNK_SIZE;
			var p2 = cameraPos / Chunk.CHUNK_SIZE;

			var minf = Vector3.Min(p1, p2);
			var maxf = Vector3.Max(p1, p2);

			var min = minf.ToFloorIntVector3();
			var max = maxf.ToFloorIntVector3();

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

		readonly int VERTEX_CACHE_COUNT = Environment.ProcessorCount * 2;
		BlockingCollection<VertexListCacheItem> m_vertexLists;
		BlockingCollection<VertexListCacheItem> m_chunkList;

		public void Update(GameTime gameTime)
		{
			if (m_vertexLists == null)
			{
				var stack = new ConcurrentStack<VertexListCacheItem>();
				m_vertexLists = new BlockingCollection<VertexListCacheItem>(stack);

				for (int i = 0; i < VERTEX_CACHE_COUNT; ++i)
					m_vertexLists.Add(new VertexListCacheItem());

				var queue = new ConcurrentQueue<VertexListCacheItem>();
				m_chunkList = new BlockingCollection<VertexListCacheItem>(queue);
			}

			InvalidateDueVisibilityChange();

			var task = Task.Run(() =>
			{
				var frustum = m_camera.Frustum;

				var eyePos = m_camera.Position;

				Parallel.ForEach(m_chunks, chunk =>
				{
					var res = frustum.Contains(ref chunk.BBox);

					if (res == ContainmentType.Disjoint)
					{
						if (chunk.IsEnabled)
						{
							chunk.IsEnabled = false;
							chunk.Free();
						}
					}
					else
					{
						chunk.IsEnabled = true;

						if (chunk.IsInvalid)
						{
							Interlocked.Increment(ref m_chunkRecalcs);

							VertexListCacheItem cacheItem;

							cacheItem = m_vertexLists.Take();

							chunk.GenerateVertices(m_scene, eyePos, cacheItem.TerrainVertexList, cacheItem.SceneryVertexList);

							cacheItem.Chunk = chunk;
							m_chunkList.Add(cacheItem);
						}
					}
				});

				m_chunkList.Add(null);
			});

			VertexListCacheItem item;

			while (m_chunkList.TryTake(out item, -1))
			{
				if (item == null)
					break;

				var chunk = item.Chunk;

				chunk.UpdateVertexBuffer(m_scene.Game.GraphicsDevice, item.TerrainVertexList);
				chunk.UpdateSceneryVertexBuffer(m_scene.Game.GraphicsDevice, item.SceneryVertexList);

				item.Chunk = null;
				m_vertexLists.Add(item);
			}

			task.Wait();
			task.Dispose();

			System.Diagnostics.Trace.Assert(m_vertexLists.Count == VERTEX_CACHE_COUNT);
			System.Diagnostics.Trace.Assert(m_chunkList.Count == 0);
		}

		public void Draw(GameTime gameTime)
		{
			var device = m_scene.Game.GraphicsDevice;

			int numVertices = 0;
			int numChunks = 0;

			foreach (var chunk in m_chunks)
			{
				if (chunk.IsEnabled == false)
					continue;

				m_scene.Effect.SetPerObjectConstBuf(chunk.ChunkOffset);

				chunk.DrawTerrain(device);

				numVertices += chunk.VertexCount;
				numChunks++;
			}

			this.VerticesRendered = numVertices;
			this.ChunksRendered = numChunks;
		}

		public void DrawTrees()
		{
			var device = m_scene.Game.GraphicsDevice;

			foreach (var chunk in m_chunks)
			{
				if (chunk.IsEnabled == false)
					continue;

				m_scene.SymbolEffect.SetPerObjectConstBuf(chunk.ChunkOffset);

				chunk.DrawTrees(device);
			}
		}
	}
}
