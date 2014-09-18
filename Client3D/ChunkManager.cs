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

		List<Chunk> m_nearList = new List<Chunk>();
		List<Chunk> m_rebuildList = new List<Chunk>();
		List<Chunk> m_drawList = new List<Chunk>();

		public string ChunkCountDebug
		{
			get
			{
				return string.Format("{0}/{1}/{2}",
					m_drawList.Count, m_nearList.Count, m_chunks.Length);
			}
		}

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

		bool m_forceNearListUpdate;
		bool m_forceDrawListUpdate;

		public ChunkManager(TerrainRenderer scene)
		{
			m_scene = scene;

			CreateChunks();
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);

			foreach (var chunk in m_chunks)
				chunk.Free();
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

			m_cameraPos = m_camera.Position;
			m_cameraLook = m_camera.Look;
			m_cameraChunkPos = (m_cameraPos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			m_forceNearListUpdate = true;
			m_forceDrawListUpdate = true;
		}

		void InvalidateChunk(Chunk chunk)
		{
			chunk.IsValid = false;
			// XXX drawlist update will add visible invalid chunks to rebuild list. is there a cleaner way?
			m_forceDrawListUpdate = true;
		}

		public void InvalidateChunks()
		{
			foreach (var chunk in m_chunks)
				InvalidateChunk(chunk);
		}

		void InvalidateChunk(IntVector3 cp)
		{
			var chunk = m_chunks[this.Size.GetIndex(cp)];
			InvalidateChunk(chunk);
		}

		public void InvalidateChunksZ(int fromZ, int toZ)
		{
			int fromIdx = this.Size.GetIndex(0, 0, fromZ / Chunk.CHUNK_SIZE);
			int toIdx = this.Size.GetIndex(0, 0, (toZ / Chunk.CHUNK_SIZE) + 1);

			for (int idx = fromIdx; idx < toIdx; ++idx)
			{
				var chunk = m_chunks[idx];
				InvalidateChunk(chunk);
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

		void InvalidateDueVisibilityChange(IntVector3 cameraChunkPos)
		{
			var min = IntVector3.Min(cameraChunkPos, m_cameraChunkPos);
			var max = IntVector3.Max(cameraChunkPos, m_cameraChunkPos);

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
		}

		readonly int VERTEX_CACHE_COUNT = Environment.ProcessorCount * 2;
		BlockingCollection<VertexListCacheItem> m_vertexCacheStack;
		BlockingCollection<VertexListCacheItem> m_vertexCacheQueue;

		IntVector3 m_cameraChunkPos;
		Vector3 m_cameraPos;
		Vector3 m_cameraLook;

		void UpdateNearList()
		{
			m_nearList.Clear();

			var eye = m_camera.Position;

			var farCorner = m_camera.Frustum.GetCorners()[4];
			float camRadius = (farCorner - eye).Length();

			float chunkRadius = (float)Math.Sqrt(3) * Chunk.CHUNK_SIZE / 2;

			foreach (var chunk in m_chunks)
			{
				var chunkCenter = chunk.ChunkOffset.ToVector3() + new Vector3(Chunk.CHUNK_SIZE / 2);

				if (Vector3.Distance(eye, chunkCenter) - chunkRadius <= camRadius)
				{
					m_nearList.Add(chunk);
				}
				else
				{
					chunk.Free();
					chunk.IsValid = false;
				}
			}

			m_forceDrawListUpdate = true;
		}

		void UpdateDrawList()
		{
			m_drawList.Clear();

			var frustum = m_camera.Frustum;

			foreach (var chunk in m_nearList)
			{
				var res = frustum.Contains(ref chunk.BBox);

				if (res == ContainmentType.Disjoint)
					continue;

				if (!chunk.IsValid)
					m_rebuildList.Add(chunk);

				m_drawList.Add(chunk);
			}
		}

		void ProcessRebuildList(IntVector3 cameraChunkPos)
		{
			if (m_rebuildList.Count == 0)
				return;

			if (m_vertexCacheStack == null)
			{
				var stack = new ConcurrentStack<VertexListCacheItem>();
				m_vertexCacheStack = new BlockingCollection<VertexListCacheItem>(stack);

				for (int i = 0; i < VERTEX_CACHE_COUNT; ++i)
					m_vertexCacheStack.Add(new VertexListCacheItem());

				var queue = new ConcurrentQueue<VertexListCacheItem>();
				m_vertexCacheQueue = new BlockingCollection<VertexListCacheItem>(queue);
			}

			var task = Task.Run(() =>
			{
				Parallel.ForEach(m_rebuildList, chunk =>
				{
					var cacheItem = m_vertexCacheStack.Take();

					chunk.GenerateVertices(m_scene, cameraChunkPos, cacheItem.TerrainVertexList, cacheItem.SceneryVertexList);

					cacheItem.Chunk = chunk;
					m_vertexCacheQueue.Add(cacheItem);
				});

				m_vertexCacheQueue.Add(null);
			});

			while (true)
			{
				var cacheItem = m_vertexCacheQueue.Take();

				if (cacheItem == null)
					break;

				var chunk = cacheItem.Chunk;

				chunk.UpdateVertexBuffer(m_scene.Game.GraphicsDevice, cacheItem.TerrainVertexList);
				chunk.UpdateSceneryVertexBuffer(m_scene.Game.GraphicsDevice, cacheItem.SceneryVertexList);

				chunk.IsValid = true;

				cacheItem.Chunk = null;
				m_vertexCacheStack.Add(cacheItem);

				m_chunkRecalcs++;
			}

			task.Wait();
			task.Dispose();

			System.Diagnostics.Trace.Assert(m_vertexCacheStack.Count == VERTEX_CACHE_COUNT);
			System.Diagnostics.Trace.Assert(m_vertexCacheQueue.Count == 0);

			m_rebuildList.Clear();
		}

		public void Update(GameTime gameTime)
		{
			var cameraPos = m_camera.Position;
			var cameraLook = m_camera.Look;
			var cameraChunkPos = (cameraPos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			if (m_cameraChunkPos != cameraChunkPos)
				InvalidateDueVisibilityChange(cameraChunkPos);

			if (m_forceNearListUpdate || m_cameraChunkPos != cameraChunkPos)
				UpdateNearList();

			if (m_forceDrawListUpdate || m_cameraPos != cameraPos || m_cameraLook != cameraLook)
				UpdateDrawList();

			ProcessRebuildList(cameraChunkPos);

			m_cameraPos = cameraPos;
			m_cameraLook = cameraLook;
			m_cameraChunkPos = cameraChunkPos;

			m_forceNearListUpdate = m_forceDrawListUpdate = false;
		}

		public void Draw(GameTime gameTime)
		{
			var device = m_scene.Game.GraphicsDevice;

			int numVertices = 0;
			int numChunks = 0;

			foreach (var chunk in m_drawList)
			{
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

			foreach (var chunk in m_drawList)
			{
				m_scene.SymbolEffect.SetPerObjectConstBuf(chunk.ChunkOffset);

				chunk.DrawTrees(device);
			}
		}
	}
}
