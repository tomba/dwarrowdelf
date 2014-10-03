using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Dwarrowdelf;
using Dwarrowdelf.Client;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;

namespace Client3D
{
	class Chunk
	{
		public const int CHUNK_SIZE = 16;
		public const int VOXELS_PER_CHUNK = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
		public const int MAX_VERTICES_PER_CHUNK = VOXELS_PER_CHUNK * 6;

		public static readonly IntSize3 ChunkSize = new IntSize3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE);

		VoxelMap m_map;

		/// <summary>
		/// Chunk position
		/// </summary>
		public IntVector3 ChunkPosition { get; private set; }
		/// <summary>
		/// Chunk offset, i.e. position * CHUNK_SIZE
		/// </summary>
		public IntVector3 ChunkOffset { get; private set; }

		// Maximum number of vertices this Chunk has had
		int m_maxVertices;

		public bool IsValid { get; set; }

		/// <summary>
		/// The chunk contains only hidden voxels
		/// </summary>
		public bool IsHidden { get; set; }

		Buffer<TerrainVertex> m_vertexBuffer;
		public int VertexCount { get; private set; }

		Buffer<SceneryVertex> m_sceneryVertexBuffer;
		public int SceneryVertexCount { get; private set; }

		public BoundingBox BBox;

		public static Chunk CreateOrNull(VoxelMap map, IntVector3 chunkPosition)
		{
			var chunkOffset = chunkPosition * CHUNK_SIZE;

			bool isEmpty, isHidden;

			CheckIfEmptyOrHidden(map, chunkOffset, out isHidden, out isEmpty);

			if (isEmpty)
				return null;

			var chunk = new Chunk(map, chunkPosition);
			chunk.IsHidden = isHidden;

			return chunk;
		}

		Chunk(VoxelMap map, IntVector3 chunkPosition)
		{
			this.ChunkPosition = chunkPosition;
			this.ChunkOffset = chunkPosition * CHUNK_SIZE;

			m_map = map;

			var v1 = this.ChunkOffset.ToVector3();
			var v2 = v1 + new Vector3(Chunk.CHUNK_SIZE);
			this.BBox = new BoundingBox(v1, v2);
		}

		static void CheckIfEmptyOrHidden(VoxelMap map, IntVector3 chunkOffset, out bool isHidden, out bool isEmpty)
		{
			int x0 = chunkOffset.X;
			int x1 = chunkOffset.X + CHUNK_SIZE - 1;

			int y0 = chunkOffset.Y;
			int y1 = chunkOffset.Y + CHUNK_SIZE - 1;

			int z0 = chunkOffset.Z;
			int z1 = chunkOffset.Z + CHUNK_SIZE - 1;

			uint current = map.Grid[z0, y0, x0].Raw;

			for (int z = z0; z <= z1; ++z)
			{
				for (int y = y0; y <= y1; ++y)
				{
					for (int x = x0; x <= x1; ++x)
					{
						if (current != map.Grid[z, y, x].Raw)
						{
							isHidden = false;
							isEmpty = false;
							return;
						}
					}
				}
			}

			Voxel vox = new Voxel() { Raw = current };

			isEmpty = vox.IsEmpty;
			isHidden = vox.VisibleFaces == 0;
		}

		public void Free()
		{
			Utilities.Dispose(ref m_vertexBuffer);
			Utilities.Dispose(ref m_sceneryVertexBuffer);
		}

		public void UpdateVertexBuffer(GraphicsDevice device, VertexList<TerrainVertex> vertexList)
		{
			if (vertexList.Count == 0)
				return;

			if (m_vertexBuffer == null || m_vertexBuffer.ElementCount < vertexList.Count)
			{
				if (vertexList.Count > m_maxVertices)
					m_maxVertices = vertexList.Count;

				//System.Diagnostics.Trace.TraceError("Alloc {0}: {1} verts", this.ChunkOffset, m_maxVertices);

				Utilities.Dispose(ref m_vertexBuffer);
				m_vertexBuffer = Buffer.Vertex.New<TerrainVertex>(device, m_maxVertices);
			}

			m_vertexBuffer.SetData(vertexList.Data, 0, vertexList.Count);
		}

		public void UpdateSceneryVertexBuffer(GraphicsDevice device, VertexList<SceneryVertex> vertexList)
		{
			if (vertexList.Count == 0)
				return;

			if (m_sceneryVertexBuffer == null || m_sceneryVertexBuffer.ElementCount < vertexList.Count)
			{
				Utilities.Dispose(ref m_sceneryVertexBuffer);
				m_sceneryVertexBuffer = Buffer.Vertex.New<SceneryVertex>(device, vertexList.Data.Length);
			}

			m_sceneryVertexBuffer.SetData(vertexList.Data, 0, vertexList.Count);
		}

		public void DrawTerrain(GraphicsDevice device)
		{
			if (this.VertexCount == 0)
				return;

			device.SetVertexBuffer(m_vertexBuffer);
			device.Draw(PrimitiveType.PointList, this.VertexCount);
		}

		public void DrawTrees(GraphicsDevice device)
		{
			if (this.SceneryVertexCount == 0)
				return;

			device.SetVertexBuffer(m_sceneryVertexBuffer);
			device.Draw(PrimitiveType.PointList, this.SceneryVertexCount);
		}

		public void GenerateVertices(ref IntGrid3 viewGrid, IntVector3 cameraChunkPos,
			VertexList<TerrainVertex> terrainVertexList, VertexList<SceneryVertex> sceneryVertexList)
		{
			terrainVertexList.Clear();
			sceneryVertexList.Clear();

			var diff = cameraChunkPos - this.ChunkPosition;

			FaceDirectionBits visibleChunkFaces = 0;
			if (diff.X >= 0)
				visibleChunkFaces |= FaceDirectionBits.PositiveX;
			if (diff.X <= 0)
				visibleChunkFaces |= FaceDirectionBits.NegativeX;
			if (diff.Y >= 0)
				visibleChunkFaces |= FaceDirectionBits.PositiveY;
			if (diff.Y <= 0)
				visibleChunkFaces |= FaceDirectionBits.NegativeY;
			if (diff.Z >= 0)
				visibleChunkFaces |= FaceDirectionBits.PositiveZ;
			if (diff.Z <= 0)
				visibleChunkFaces |= FaceDirectionBits.NegativeZ;

			GenerateVertices(ref viewGrid, visibleChunkFaces, terrainVertexList, sceneryVertexList);

			this.VertexCount = terrainVertexList.Count;
			this.SceneryVertexCount = sceneryVertexList.Count;
		}

		void GenerateVertices(ref IntGrid3 viewGrid, FaceDirectionBits visibleChunkFaces,
			VertexList<TerrainVertex> terrainVertexList,
			VertexList<SceneryVertex> sceneryVertexList)
		{
			IntGrid3 chunkGrid = viewGrid.Intersect(new IntGrid3(this.ChunkOffset, Chunk.ChunkSize));

			// is the chunk inside frustum, but outside the viewgrid?
			if (chunkGrid.IsNull)
				return;

			if (this.IsHidden)
			{
				CreateUndefinedChunk(ref viewGrid, ref chunkGrid, terrainVertexList, visibleChunkFaces);
				return;
			}

			// Draw from up to down to avoid overdraw
			for (int z = chunkGrid.Z2; z >= chunkGrid.Z1; --z)
			{
				for (int y = chunkGrid.Y1; y <= chunkGrid.Y2; ++y)
				{
					for (int x = chunkGrid.X1; x <= chunkGrid.X2; ++x)
					{
						var vox = m_map.Grid[z, y, x];

						if (vox.IsEmpty)
							continue;

						var p = new IntVector3(x, y, z);

						if ((vox.Flags & VoxelFlags.Tree) != 0)
						{
							var pos = p - this.ChunkOffset;
							sceneryVertexList.Add(new SceneryVertex(pos.ToVector3(), Color.LightGreen,
								(int)Dwarrowdelf.Client.SymbolID.ConiferousTree));

							if (vox.Type != VoxelType.Empty)
								throw new Exception();

							continue; ;
						}

						if (vox.Type == VoxelType.Empty)
							continue;

						HandleVoxel(p, ref vox, ref viewGrid, visibleChunkFaces, terrainVertexList);
					}
				}
			}
		}

		void CreateUndefinedChunk(ref IntGrid3 viewGrid, ref IntGrid3 chunkGrid, VertexList<TerrainVertex> vertexList,
			FaceDirectionBits visibleChunkFaces)
		{
			// clear the visible chunk faces that are not at the view's edge

			// up
			if ((visibleChunkFaces & FaceDirectionBits.PositiveZ) != 0 && chunkGrid.Z2 != viewGrid.Z2)
				visibleChunkFaces &= ~FaceDirectionBits.PositiveZ;

			// down
			// Note: we never draw the bottommost layer in the map
			visibleChunkFaces &= ~FaceDirectionBits.NegativeZ;

			// east
			if ((visibleChunkFaces & FaceDirectionBits.PositiveX) != 0 && chunkGrid.X2 != viewGrid.X2)
				visibleChunkFaces &= ~FaceDirectionBits.PositiveX;

			// west
			if ((visibleChunkFaces & FaceDirectionBits.NegativeX) != 0 && chunkGrid.X1 != viewGrid.X1)
				visibleChunkFaces &= ~FaceDirectionBits.NegativeX;

			// south
			if ((visibleChunkFaces & FaceDirectionBits.PositiveY) != 0 && chunkGrid.Y2 != viewGrid.Y2)
				visibleChunkFaces &= ~FaceDirectionBits.PositiveY;

			// north
			if ((visibleChunkFaces & FaceDirectionBits.NegativeY) != 0 && chunkGrid.Y1 != viewGrid.Y1)
				visibleChunkFaces &= ~FaceDirectionBits.NegativeY;

			if (visibleChunkFaces == 0)
				return;

			int sides = (int)visibleChunkFaces;

			var tex = new FaceTexture()
			{
				Symbol1 = SymbolID.Unknown,
				Color1 = GameColor.LightGray,
			};

			const int occlusion = 4;

#if BIG_CHUNK
			/* Using chunk sized quads causes t-junction problems */

			var scale = new IntVector3(chunkGrid.Size.Width, chunkGrid.Size.Height, chunkGrid.Size.Depth);
			var offset = chunkGrid.Corner1 - this.ChunkOffset;

			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				var vertices = s_cubeFaceInfo[side].Vertices;

				IntVector3 v0 = vertices[0] * scale + offset;
				IntVector3 v1 = vertices[1] * scale + offset;
				IntVector3 v2 = vertices[2] * scale + offset;
				IntVector3 v3 = vertices[3] * scale + offset;

				var vd = new TerrainVertex(v0, v1, v2, v3, occlusion, occlusion, occlusion, occlusion, tex);
				vertexList.Add(vd);
			}
#else
			var offset = chunkGrid.Corner1 - this.ChunkOffset;

			var dim = new IntVector3(chunkGrid.Size.Width, chunkGrid.Size.Height, chunkGrid.Size.Depth);

			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				int d0 = side / 2;
				int d1 = (d0 + 1) % 3;
				int d2 = (d0 + 2) % 3;

				bool posFace = (side & 1) == 0;

				var vertices = s_cubeFaceInfo[side].Vertices;

				IntVector3 v0 = vertices[0] + offset;
				IntVector3 v1 = vertices[1] + offset;
				IntVector3 v2 = vertices[2] + offset;
				IntVector3 v3 = vertices[3] + offset;

				var vec1 = new IntVector3();
				vec1[d1] = 1;

				var vec2 = new IntVector3();
				vec2[d2] = 1;

				for (int v = 0; v < dim[d1]; ++v)
					for (int u = 0; u < dim[d2]; ++u)
					{
						var off = vec1 * v + vec2 * u;
						if (posFace)
							off[d0] = dim[d0] - 1;

						var vd = new TerrainVertex(v0 + off, v1 + off, v2 + off, v3 + off,
							occlusion, occlusion, occlusion, occlusion, tex);
						vertexList.Add(vd);
					}
			}
#endif
		}

		void GetTextures(ref Voxel vox, out FaceTexture baseTexture, out FaceTexture topTexture)
		{
			baseTexture = new FaceTexture();
			topTexture = new FaceTexture();

			if (vox.VisibleFaces == 0)
			{
				baseTexture.Symbol1 = SymbolID.Unknown;
				baseTexture.Color1 = GameColor.LightGray;
				return;
			}

			switch (vox.Type)
			{
				case VoxelType.Water:
					baseTexture.Symbol1 = SymbolID.Water;
					baseTexture.Color0 = GameColor.MediumBlue;
					baseTexture.Color1 = GameColor.SeaGreen;
					topTexture = baseTexture;
					break;

				case VoxelType.Rock:
					baseTexture.Color0 = GameColor.LightGray;
					baseTexture.Symbol1 = SymbolID.Wall;
					baseTexture.Color1 = GameColor.LightGray;

					if ((vox.Flags & VoxelFlags.Grass) != 0)
					{
						topTexture.Color0 = GameColor.LightGreen;
						topTexture.Symbol1 = SymbolID.Grass;
						topTexture.Color1 = GameColor.LightGreen;
					}
					else if ((vox.VisibleFaces & FaceDirectionBits.PositiveZ) != 0)
					{
						topTexture.Color0 = GameColor.LightGray;
						topTexture.Symbol1 = SymbolID.Floor;
						topTexture.Color1 = GameColor.LightGray;
					}
					else
					{
						topTexture = baseTexture;
					}
					break;

				default:
					throw new Exception();
			}
		}

		void HandleVoxel(IntVector3 p, ref Voxel vox, ref IntGrid3 viewGrid, FaceDirectionBits visibleChunkFaces,
			VertexList<TerrainVertex> vertexList)
		{
			FaceTexture baseTexture, topTexture;

			GetTextures(ref vox, out baseTexture, out topTexture);

			int x = p.X;
			int y = p.Y;
			int z = p.Z;

			FaceDirectionBits visibleFaces = visibleChunkFaces & vox.VisibleFaces;
			/* sides that are shown due to the viewgrid, but are really hidden by other voxels */
			FaceDirectionBits visibleHiddenFaces = 0;

			// up
			if ((visibleChunkFaces & FaceDirectionBits.PositiveZ) != 0 && z == viewGrid.Z2)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveZ;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
				// override the top tex to remove the grass
				topTexture = baseTexture;
			}

			// down
			// Note: we never draw the bottommost layer in the map
			if (z == 0)
				visibleFaces &= ~FaceDirectionBits.NegativeZ;

			// east
			if ((visibleChunkFaces & FaceDirectionBits.PositiveX) != 0 && x == viewGrid.X2)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveX;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// west
			if ((visibleChunkFaces & FaceDirectionBits.NegativeX) != 0 && x == viewGrid.X1)
			{
				const FaceDirectionBits b = FaceDirectionBits.NegativeX;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// south
			if ((visibleChunkFaces & FaceDirectionBits.PositiveY) != 0 && y == viewGrid.Y2)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveY;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// north
			if ((visibleChunkFaces & FaceDirectionBits.NegativeY) != 0 && y == viewGrid.Y1)
			{
				const FaceDirectionBits b = FaceDirectionBits.NegativeY;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			if (visibleFaces == 0)
				return;

			CreateCube(p, visibleFaces, visibleHiddenFaces, ref baseTexture, ref topTexture, vertexList);
		}

		void CreateCube(IntVector3 p, FaceDirectionBits visibleFaces, FaceDirectionBits visibleHiddenFaces,
			ref FaceTexture baseTexture, ref FaceTexture topTexture, VertexList<TerrainVertex> vertexList)
		{
			var offset = p - this.ChunkOffset;

			int sides = (int)visibleFaces;

			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				var vertices = s_cubeFaceInfo[side].Vertices;

				IntVector3 v0, v1, v2, v3;

				v0 = vertices[0] + offset;
				v1 = vertices[1] + offset;
				v2 = vertices[2] + offset;
				v3 = vertices[3] + offset;

				int occ0, occ1, occ2, occ3;

				if (((int)visibleHiddenFaces & (1 << side)) != 0)
				{
					occ0 = occ1 = occ2 = occ3 = 4;
				}
				else
				{
					GetOcclusionsForFace(p, (FaceDirection)side,
						out occ0, out occ1, out occ2, out occ3);
				}
				var vd = new TerrainVertex(v0, v1, v2, v3, occ0, occ1, occ2, occ3,
					side == (int)FaceDirection.PositiveZ ? topTexture : baseTexture);
				vertexList.Add(vd);
			}
		}

		bool IsBlocker(IntVector3 p)
		{
			if (m_map.Size.Contains(p) == false)
				return false;

			return m_map.Grid[p.Z, p.Y, p.X].IsOpaque;
		}

		void GetOcclusionsForFace(IntVector3 p, FaceDirection face,
			out int o0, out int o1, out int o2, out int o3)
		{
			var odata = s_cubeFaceInfo[(int)face].OcclusionVectors;

			o0 = o1 = o2 = o3 = 0;

			bool b_edge2 = IsBlocker(p + odata[0]);

			for (int i = 0; i < 4; ++i)
			{
				bool b_edge1 = b_edge2;
				bool b_corner = IsBlocker(p + odata[i * 2 + 1]);
				b_edge2 = IsBlocker(p + odata[(i * 2 + 2) % 8]);

				int occlusion;

				if (b_edge1 && b_edge2)
				{
					occlusion = 3;
				}
				else
				{
					occlusion = 0;

					if (b_edge1)
						occlusion++;
					if (b_edge2)
						occlusion++;
					if (b_corner)
						occlusion++;
				}

				switch (i)
				{
					case 0:
						o0 = occlusion; break;
					case 1:
						o1 = occlusion; break;
					case 2:
						o2 = occlusion; break;
					case 3:
						o3 = occlusion; break;
					default:
						throw new Exception();
				}
			}
		}

		static CubeFaceInfo CreateFaceInfo(Direction normalDir, Direction upDir, Direction rightDir)
		{
			var normal = new IntVector3(normalDir);
			var up = new IntVector3(upDir);
			var right = new IntVector3(rightDir);

			var topRight = up + right;
			var bottomRight = -up + right;
			var bottomLeft = -up - right;
			var topLeft = up - right;

			var vertices =
				new[] { topRight, bottomRight, bottomLeft, topLeft, }
				.Select(v => v + normal)								// add normal to lift from origin
				.Select(v => v + new IntVector3(1, 1, 1))				// translate to [0,2]
				.Select(v => v / 2)										// scale to [0,1]
				.ToArray();

			var occlusionVectors = new[] {
				up,
				up + right,
				right,
				-up + right,
				-up,
				-up - right,
				-right,
				up - right,
			}.Select(v => v + normal).ToArray();

			return new CubeFaceInfo(vertices, occlusionVectors);
		}

		/// <summary>
		/// Cube face infos, in the same order as FaceDirection enum
		/// </summary>
		static CubeFaceInfo[] s_cubeFaceInfo =
		{
			CreateFaceInfo(Direction.East, Direction.Up, Direction.North),
			CreateFaceInfo(Direction.West, Direction.Up, Direction.South),
			CreateFaceInfo(Direction.South, Direction.Up, Direction.East),
			CreateFaceInfo(Direction.North, Direction.Up, Direction.West),
			CreateFaceInfo(Direction.Up, Direction.North, Direction.East),
			CreateFaceInfo(Direction.Down, Direction.North, Direction.West),
		};

		class CubeFaceInfo
		{
			public CubeFaceInfo(IntVector3[] vertices, IntVector3[] occlusionVectors)
			{
				this.Vertices = vertices;
				this.OcclusionVectors = occlusionVectors;
			}

			/// <summary>
			/// Face vertices in [0,1] range
			/// </summary>
			public readonly IntVector3[] Vertices;

			/// <summary>
			/// Occlusion help vectors. Vectors point to occlusing neighbors in clockwise order, starting from top.
			/// </summary>
			public readonly IntVector3[] OcclusionVectors;
		}
	}
}
