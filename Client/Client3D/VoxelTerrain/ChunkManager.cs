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

namespace Dwarrowdelf.Client
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

		// XXX used by outlinerenderer
		public Chunk[] DebugChunks { get { return m_chunks; } }

		Chunk[] m_chunks;

		// nearlist contains all chunks within view radius, which are also inside the viewgrid
		List<Chunk> m_nearList = new List<Chunk>();

		/* rebuildlist contains all chunks that need to be rebuilt */
		List<Chunk> m_rebuildList = new List<Chunk>();

		/* drawlist contains all chunks inside frustum */
		List<Chunk> m_drawList = new List<Chunk>();

		public string ChunkCountDebug
		{
			get
			{
				return string.Format("{0}/{1}/{2}/{3}",
					m_drawList.Count, m_nearList.Count, m_chunks.Where(c => c != null).Count(), m_chunks.Length);
			}
		}

		TerrainRenderer m_scene;

		public int VerticesRendered { get; private set; }
		public int ChunkRecalcs { get; set; }

		/// <summary>
		/// Size in chunks
		/// </summary>
		public IntSize3 Size { get; private set; }

		CameraProvider m_camera;

		bool m_forceNearListUpdate;
		bool m_forceDrawListUpdate;

		public ChunkManager(TerrainRenderer scene)
		{
			m_scene = scene;

			var map = GameData.Data.Map;
			this.Size = map.Size / Chunk.CHUNK_SIZE;
			m_chunks = new Chunk[this.Size.Volume];

			var viewGridProvider = m_scene.Services.GetService<ViewGridProvider>();
			viewGridProvider.ViewGridCornerChanged += OnViewGridCornerChanged;
		}

		void OnViewGridCornerChanged(IntVector3 oldValue, IntVector3 newValue)
		{
			var diff = newValue - oldValue;

			if (diff.X == 0 && diff.Y == 0)
				InvalidateChunksZ(Math.Min(oldValue.Z, newValue.Z), Math.Max(oldValue.Z, newValue.Z));
			else
				InvalidateChunks();

			m_forceNearListUpdate = true;
			m_forceDrawListUpdate = true;
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);

			foreach (var chunk in m_chunks)
				if (chunk != null)
					chunk.Free();
		}

		public void Initialize()
		{
			m_camera = m_scene.Services.GetService<CameraProvider>();

			m_cameraPos = m_camera.Position;
			m_cameraLook = m_camera.Look;
			m_cameraChunkPos = (m_cameraPos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			m_forceNearListUpdate = true;
			m_forceDrawListUpdate = true;

			GameData.Data.Map.TileChanged += OnTileChanged;
		}

		void SetChunk(IntVector3 cp, Chunk chunk)
		{
			m_chunks[this.Size.GetIndex(cp)] = chunk;
		}

		Chunk GetChunk(IntVector3 cp)
		{
			return m_chunks[this.Size.GetIndex(cp)];
		}

		void FreeChunk(Chunk chunk)
		{
			chunk.Free();
			SetChunk(chunk.ChunkPosition, null);
		}

		Chunk CreateChunk(IntVector3 cp)
		{
			var chunk = new Chunk(GameData.Data.Map, cp);
			SetChunk(chunk.ChunkPosition, chunk);
			return chunk;
		}

		void OnTileChanged(IntVector3 p)
		{
			var cp = p / Chunk.CHUNK_SIZE;

			var chunk = GetChunk(cp);

			if (chunk == null)
				return;

			chunk.UpdateVoxel(p);

			InvalidateChunk(chunk);
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
				if (chunk != null)
					InvalidateChunk(chunk);
		}

		void InvalidateChunk(IntVector3 cp)
		{
			var chunk = GetChunk(cp);
			if (chunk != null)
				InvalidateChunk(chunk);
		}

		public void InvalidateChunksZ(int fromZ, int toZ)
		{
			int fromIdx = this.Size.GetIndex(0, 0, fromZ / Chunk.CHUNK_SIZE);
			int toIdx = this.Size.GetIndex(0, 0, (toZ / Chunk.CHUNK_SIZE) + 1);

			for (int idx = fromIdx; idx < toIdx; ++idx)
			{
				var chunk = m_chunks[idx];
				if (chunk != null)
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

		readonly static int VERTEX_CACHE_COUNT = Environment.ProcessorCount * 2;

		BlockingBufferPool<VertexListCacheItem> m_vertexCacheStack =
			new BlockingBufferPool<VertexListCacheItem>(VERTEX_CACHE_COUNT, () => new VertexListCacheItem());
		BlockingCollection<VertexListCacheItem> m_vertexCacheQueue =
			new BlockingCollection<VertexListCacheItem>(new ConcurrentQueue<VertexListCacheItem>());

		IntVector3 m_cameraChunkPos;
		Vector3 m_cameraPos;
		Vector3 m_cameraLook;

		static Vector3 GetFrustumFarthestCorner(ref BoundingFrustum frustum)
		{
			Plane p1 = frustum.Far;
			Plane p2 = frustum.Bottom;
			Plane p3 = frustum.Right;

			var num = -p1.D * Vector3.Cross(p2.Normal, p3.Normal) - p2.D * Vector3.Cross(p3.Normal, p1.Normal) - p3.D * Vector3.Cross(p1.Normal, p2.Normal);
			var denom = Vector3.Dot(p1.Normal, Vector3.Cross(p2.Normal, p3.Normal));

			return num / denom;
		}

		void UpdateNearList()
		{
			m_nearList.Clear();

			var eye = m_camera.Position;
			var frustum = m_camera.Frustum;

			var farCorner = GetFrustumFarthestCorner(ref frustum);

			float camRadius = (farCorner - eye).Length();

			float chunkRadius = (float)Math.Sqrt(3) * Chunk.CHUNK_SIZE / 2;

			var viewGrid = m_scene.Services.GetService<ViewGridProvider>().ViewGrid;

			// XXX grid can be reduced to be inside camradius, but we need to somehow free the chunks that go outside near list
			//var grid = new IntGrid3(viewGrid.Corner1 / Chunk.CHUNK_SIZE, (viewGrid.Corner2 + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE);
			var grid = this.Size;

			foreach (var cp in grid.Range())
			{
				var chunk = GetChunk(cp);

				IntVector3 chunkOffset = cp * Chunk.CHUNK_SIZE;

				var chunkGrid = new IntGrid3(chunkOffset, Chunk.ChunkSize);

				var containment = viewGrid.ContainsExclusive(ref chunkGrid);

				if (containment == Containment.Disjoint)
				{
					// the chunk is outside the view area
					if (chunk != null)
						FreeChunk(chunk);
					continue;
				}

				var chunkCenter = chunkOffset.ToVector3() + new Vector3(Chunk.CHUNK_SIZE / 2);

				if (Vector3.Distance(eye, chunkCenter) - chunkRadius > camRadius)
				{
					if (chunk != null)
						FreeChunk(chunk);
					continue;
				}

				if (chunk == null)
					chunk = CreateChunk(cp);

				m_nearList.Add(chunk);
			}

			m_forceDrawListUpdate = true;
		}

		void UpdateDrawList()
		{
			m_drawList.Clear();

			var frustum = m_camera.Frustum;

			Parallel.ForEach(m_nearList, chunk =>
			{
				var res = frustum.Contains(ref chunk.BBox);

				if (res == ContainmentType.Disjoint)
					return;

				if (!chunk.IsValid)
				{
					lock (m_rebuildList)
						m_rebuildList.Add(chunk);
				}

				lock (m_drawList)
					m_drawList.Add(chunk);
			});
		}

		void ProcessRebuildList(IntVector3 cameraChunkPos)
		{
			if (m_rebuildList.Count == 0)
				return;

			var task = Task.Run(() =>
			{
				var viewGridProvider = m_scene.Services.GetService<ViewGridProvider>();
				IntGrid3 viewGrid = viewGridProvider.ViewGrid;

				Parallel.ForEach(m_rebuildList, chunk =>
				{
					var cacheItem = m_vertexCacheStack.Take();

					chunk.GenerateVertices(ref viewGrid, cameraChunkPos, cacheItem.TerrainVertexList, cacheItem.SceneryVertexList);

					if (chunk.VertexCount == 0 && chunk.SceneryVertexCount == 0)
					{
						// nothing more to do, mark as valid and add back to stack
						chunk.IsValid = true;
						m_vertexCacheStack.Return(cacheItem);
					}
					else
					{
						cacheItem.Chunk = chunk;
						m_vertexCacheQueue.Add(cacheItem);
					}
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
				m_vertexCacheStack.Return(cacheItem);
			}

			task.Wait();
			task.Dispose();

			System.Diagnostics.Trace.Assert(m_vertexCacheStack.Count == VERTEX_CACHE_COUNT);
			System.Diagnostics.Trace.Assert(m_vertexCacheQueue.Count == 0);

			this.ChunkRecalcs = m_rebuildList.Count;

			m_rebuildList.Clear();
		}

		public void PrepareDraw(GameTime gameTime)
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

			foreach (var chunk in m_drawList)
			{
				if (chunk.VertexCount == 0)
					continue;

				m_scene.Effect.SetPerObjectConstBuf(chunk.ChunkOffset);

				chunk.DrawTerrain(device);

				numVertices += chunk.VertexCount;
			}

			this.VerticesRendered = numVertices;
		}

		public void DrawTrees()
		{
			var device = m_scene.Game.GraphicsDevice;

			foreach (var chunk in m_drawList)
			{
				if (chunk.SceneryVertexCount == 0)
					continue;

				m_scene.SymbolEffect.SetPerObjectConstBuf(chunk.ChunkOffset);

				chunk.DrawTrees(device);
			}
		}
	}
}
