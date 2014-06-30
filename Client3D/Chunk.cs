using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Dwarrowdelf;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Toolkit.Graphics;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;
using Dwarrowdelf.Client;

namespace Client3D
{
	class Chunk : Component
	{
		struct FaceTexture
		{
			//public SymbolID Symbol0;
			public SymbolID Symbol1;
			public SymbolID Symbol2;
			public GameColor Color0;
			public GameColor Color1;
			public GameColor Color2;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct TerrainVertex
		{
			[VertexElement("POSITION", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
			public Byte4 Position;
			[VertexElement("OCCLUSION")]
			public int Occlusion;	/* xxx pack into some other field */
			[VertexElement("TEX", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
			public Byte4 TexPack;
			[VertexElement("COLOR", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
			public Byte4 ColorPack;

			public TerrainVertex(IntVector3 pos, int occlusion, FaceTexture tex)
			{
				this.Position = new Byte4(pos.X, pos.Y, pos.Z, 0);
				this.Occlusion = occlusion;
				this.TexPack = new Byte4((byte)0, (byte)tex.Symbol1, (byte)tex.Symbol2, (byte)0);
				this.ColorPack = new Byte4((byte)tex.Color0, (byte)tex.Color1, (byte)tex.Color2, (byte)0);
			}
		}

		public const int CHUNK_SIZE = 16;
		const int MAX_TILES = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
		const int MAX_VERTICES_PER_TILE = 6 * 4;
		const int MAX_VERTICES = MAX_TILES * MAX_VERTICES_PER_TILE;

		static IntSize3 ChunkSize = new IntSize3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE);

		VoxelMap m_map;

		Buffer<TerrainVertex> m_vertexBuffer;
		static readonly VertexInputLayout s_layout = VertexInputLayout.New<TerrainVertex>(0);

		/// <summary>
		/// Chunk position
		/// </summary>
		public IntVector3 ChunkPosition { get; private set; }
		/// <summary>
		/// Chunk offset, i.e. position * CHUNK_SIZE
		/// </summary>
		public IntVector3 ChunkOffset { get; private set; }

		public int VertexCount { get; private set; }

		// Maximum number of vertices this Chunk has had
		int m_maxVertices;

		public bool IsInvalid { get; private set; }

		VertexList m_vertexList;
		bool m_vertexBufferInvalid;

		public BoundingBox BBox;

		public bool IsEnabled { get; set; }

		class VertexList
		{
			public TerrainVertex[] Data { get; private set; }
			public int Count { get; private set; }

			public VertexList(int size)
			{
				this.Data = new TerrainVertex[size];
				this.Count = 0;
			}

			public void Add(TerrainVertex data)
			{
				this.Data[this.Count++] = data;
			}

			public void Clear()
			{
				this.Count = 0;
			}
		}

		public Chunk(VoxelMap map, IntVector3 chunkPosition)
		{
			this.ChunkPosition = chunkPosition;
			this.ChunkOffset = chunkPosition * CHUNK_SIZE;

			this.IsInvalid = true;

			m_map = map;

			var v1 = this.ChunkOffset.ToVector3();
			var v2 = v1 + new Vector3(Chunk.CHUNK_SIZE);
			this.BBox = new BoundingBox(v1, v2);
		}

		public void InvalidateChunk()
		{
			this.IsInvalid = true;
		}

		public void Free()
		{
			m_vertexList = null;

			if (m_vertexBuffer != null)
			{
				//System.Diagnostics.Trace.TraceError("Free {0}", this.ChunkOffset);
				RemoveAndDispose(ref m_vertexBuffer);
				this.IsInvalid = true;
			}
		}

		public void Update(TerrainRenderer scene, Vector3 cameraPos)
		{
			if (this.IsInvalid == false)
				return;

			if (m_vertexList == null)
				m_vertexList = new VertexList(MAX_VERTICES);

			var device = scene.Game.GraphicsDevice;

			m_vertexList.Clear();

			var cameraChunkPos = (cameraPos / CHUNK_SIZE).ToFloorIntVector3();
			var diff = cameraChunkPos - this.ChunkPosition;

			FaceDirectionBits mask = 0;
			if (diff.X >= 0)
				mask |= FaceDirectionBits.PositiveX;
			if (diff.X <= 0)
				mask |= FaceDirectionBits.NegativeX;
			if (diff.Y >= 0)
				mask |= FaceDirectionBits.PositiveY;
			if (diff.Y <= 0)
				mask |= FaceDirectionBits.NegativeY;
			if (diff.Z >= 0)
				mask |= FaceDirectionBits.PositiveZ;
			if (diff.Z <= 0)
				mask |= FaceDirectionBits.NegativeZ;

			GenerateVertices(scene, mask);

			this.IsInvalid = false;
			m_vertexBufferInvalid = true;

			this.VertexCount = m_vertexList.Count;
		}

		public void Render(TerrainRenderer scene)
		{
			var device = scene.Game.GraphicsDevice;

			if (m_vertexBufferInvalid)
			{
				if (m_vertexList.Count > 0)
				{
					if (m_vertexBuffer == null || m_vertexBuffer.ElementCount < m_vertexList.Count)
					{
						if (m_vertexList.Count > m_maxVertices)
							m_maxVertices = m_vertexList.Count;

						//System.Diagnostics.Trace.TraceError("Alloc {0}: {1} verts", this.ChunkOffset, m_maxVertices);

						m_vertexBuffer = Buffer.Vertex.New<TerrainVertex>(device, m_maxVertices);
					}

					m_vertexBuffer.SetData(m_vertexList.Data, 0, m_vertexList.Count);
				}

				m_vertexBufferInvalid = false;
			}

			if (this.VertexCount > 0)
			{
				device.SetVertexBuffer(m_vertexBuffer);
				device.SetVertexInputLayout(s_layout);
				device.Draw(PrimitiveType.LineListWithAdjacency, this.VertexCount);
			}
		}

		void GenerateVertices(TerrainRenderer scene, FaceDirectionBits mask)
		{
			IntVector3 cutn = scene.ViewCorner1;
			IntVector3 cutp = scene.ViewCorner2;

			int x0 = Math.Max(cutn.X, this.ChunkOffset.X);
			int x1 = Math.Min(cutp.X, this.ChunkOffset.X + CHUNK_SIZE - 1);

			int y0 = Math.Max(cutn.Y, this.ChunkOffset.Y);
			int y1 = Math.Min(cutp.Y, this.ChunkOffset.Y + CHUNK_SIZE - 1);

			int z0 = this.ChunkOffset.Z;
			int z1 = Math.Min(cutp.Z, this.ChunkOffset.Z + CHUNK_SIZE - 1);

			// Draw from up to down to avoid overdraw
			for (int z = z1; z >= z0; --z)
			{
				for (int y = y0; y <= y1; ++y)
				{
					for (int x = x0; x <= x1; ++x)
					{
						var td = m_map.Grid[z, y, x];

						if (td.IsEmpty)
							continue;

						var p = new IntVector3(x, y, z);

						FaceTexture baseTexture = new FaceTexture();
						FaceTexture topTexture = new FaceTexture();

						if (td.IsUndefined)
						{
							baseTexture.Symbol1 = SymbolID.Unknown;
							baseTexture.Color1 = GameColor.LightGray;
						}
						else
						{
							baseTexture.Color0 = GameColor.LightGray;
							baseTexture.Symbol1 = SymbolID.Wall;
							baseTexture.Color1 = GameColor.LightGray;

							if ((td.Flags & VoxelFlags.Grass) != 0)
							{
								topTexture.Color0 = GameColor.LightGreen;
								topTexture.Symbol1 = SymbolID.Grass;
								topTexture.Color1 = GameColor.LightGreen;

								if ((td.Flags & VoxelFlags.Tree2) != 0)
								{
									topTexture.Symbol2 = SymbolID.DeciduousTree;
									topTexture.Color2 = GameColor.LightGreen;
								}
							}
							else if ((td.VisibleFaces & FaceDirectionBits.PositiveZ) != 0)
							{
								topTexture.Color0 = GameColor.LightGray;
								topTexture.Symbol1 = SymbolID.Floor;
								topTexture.Color1 = GameColor.LightGray;
							}
							else
							{
								topTexture = baseTexture;
							}
						}

						CreateCubicBlock(p, scene, baseTexture, topTexture, mask);
					}
				}
			}
		}

		void CreateCubicBlock(IntVector3 p, TerrainRenderer scene,
			FaceTexture baseTexture, FaceTexture topTexture,
			FaceDirectionBits globalFaceMask)
		{
			int x = p.X;
			int y = p.Y;
			int z = p.Z;

			var grid = m_map.Grid;

			var vd = grid[z, y, x];

			FaceDirectionBits sides = globalFaceMask & vd.VisibleFaces;
			FaceDirectionBits hiddenSides = 0;	/* sides that are shown, but are really hidden */

			// up
			if ((globalFaceMask & FaceDirectionBits.PositiveZ) != 0 && z == scene.ViewCorner2.Z)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveZ;
				hiddenSides |= b & ~sides;
				sides |= b;
				// override the top tex to remove the grass
				topTexture = baseTexture;
			}

			// down
			// Note: we never draw the bottommost layer in the map
			if (z == 0)
				sides &= ~FaceDirectionBits.NegativeZ;

			// east
			if ((globalFaceMask & FaceDirectionBits.PositiveX) != 0 && x == scene.ViewCorner2.X)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveX;
				hiddenSides |= b & ~sides;
				sides |= b;
			}

			// west
			if ((globalFaceMask & FaceDirectionBits.NegativeX) != 0 && x == scene.ViewCorner1.X)
			{
				const FaceDirectionBits b = FaceDirectionBits.NegativeX;
				hiddenSides |= b & ~sides;
				sides |= b;
			}

			// south
			if ((globalFaceMask & FaceDirectionBits.PositiveY) != 0 && y == scene.ViewCorner2.Y)
			{
				const FaceDirectionBits b = FaceDirectionBits.PositiveY;
				hiddenSides |= b & ~sides;
				sides |= b;
			}

			// north
			if ((globalFaceMask & FaceDirectionBits.NegativeY) != 0 && y == scene.ViewCorner1.Y)
			{
				const FaceDirectionBits b = FaceDirectionBits.NegativeY;
				hiddenSides |= b & ~sides;
				sides |= b;
			}

			if (sides == 0)
				return;

			CreateCube(p, sides, hiddenSides, baseTexture, topTexture);
		}

		bool IsBlocker(IntVector3 p)
		{
			if (m_map.Size.Contains(p) == false)
				return false;

			var td = m_map.Grid[p.Z, p.Y, p.X];

			if (td.IsUndefined)
				return true;

			return !td.IsEmpty;
		}

		void CreateCube(IntVector3 p, FaceDirectionBits faceMask, FaceDirectionBits hiddenFaceMask,
			FaceTexture baseTexture, FaceTexture topTexture)
		{
			var grid = m_map.Grid;

			var offset = p - this.ChunkOffset;

			int sides = (int)faceMask;

			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				var vertices = s_intCubeFaces[side];

				for (int i = 0; i < 4; ++i)
				{
					int occ;

					if (((int)hiddenFaceMask & (1 << side)) != 0)
						occ = 4;
					else
						occ = GetOcclusionForFaceVertex(p, (FaceDirection)side, s_cubeIndices[i]);

					var vd = new TerrainVertex(vertices[s_cubeIndices[i]] + offset, occ,
						side == (int)FaceDirection.PositiveZ ? topTexture : baseTexture);
					m_vertexList.Add(vd);
				}
			}
		}

		int GetOcclusionForFaceVertex(IntVector3 p, FaceDirection face, int vertexNum)
		{
			var odata = s_occlusionLookup[(int)face, vertexNum];

			bool b_corner = IsBlocker(p + odata.Corner);
			bool b_edge1 = IsBlocker(p + odata.Edge1);
			bool b_edge2 = IsBlocker(p + odata.Edge2);

			int occlusion = 0;

			if (b_edge1 && b_edge2)
				occlusion = 3;
			else
			{
				if (b_edge1)
					occlusion++;
				if (b_edge2)
					occlusion++;
				if (b_corner)
					occlusion++;
			}

			return occlusion;
		}

		/// <summary>
		/// Int cube faces from 0 to 1
		/// </summary>
		static IntVector3[][] s_intCubeFaces;

		/// <summary>
		/// Float cube faces from -0.5 to 0.5
		/// </summary>
		static Vector3[][] s_cubeFaces;

		static int[] s_cubeIndices = { 0, 1, 3, 2 };

		static Chunk()
		{
			CreateCubeFaces();

			InitOcclusionLookup();
		}

		static void CreateCubeFaces()
		{
			/*  south face */
			var south = new Vector3[] {
				new Vector3(-1,  1,  1),
				new Vector3( 1,  1,  1),
				new Vector3( 1,  1, -1),
				new Vector3(-1,  1, -1),
			};

			var rotQs = new Quaternion[]
			{
				Quaternion.RotationAxis(Vector3.UnitZ, -MathUtil.PiOverTwo),
				Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.PiOverTwo),
				Quaternion.RotationAxis(Vector3.UnitZ, 0),
				Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.Pi),
				Quaternion.RotationAxis(Vector3.UnitX, MathUtil.PiOverTwo),
				Quaternion.RotationAxis(Vector3.UnitX, -MathUtil.PiOverTwo),
			};

			s_cubeFaces = new Vector3[6][];
			s_intCubeFaces = new IntVector3[6][];

			for (int side = 0; side < 6; ++side)
			{
				Vector3[] face = new Vector3[4];
				IntVector3[] intFace = new IntVector3[4];

				for (int vn = 0; vn < 4; ++vn)
				{
					face[vn] = Vector3.Transform(south[vn], rotQs[side]) / 2.0f;
					intFace[vn] = (face[vn] + 0.5f).ToIntVector3();
				}

				s_cubeFaces[side] = face;
				s_intCubeFaces[side] = intFace;
			}
		}

		struct OcclusionLookupData
		{
			public IntVector3 Corner;
			public IntVector3 Edge1;
			public IntVector3 Edge2;
		}

		// OcclusionLookupData[face][vertexnum]
		static OcclusionLookupData[,] s_occlusionLookup;

		static void InitOcclusionLookup()
		{
			s_occlusionLookup = new OcclusionLookupData[6, 4];

			for (int face = 0; face < 6; ++face)
			{
				var cubeface = s_cubeFaces[(int)face];

				for (int vertexNum = 0; vertexNum < 4; ++vertexNum)
				{
					/*
					 * For each corner of the face, make a vector from the center of the cube through the corner and
					 * through the middles of the side edges. These vectors point to three cubes that cause occlusion.
					 */

					// corner
					var corner = cubeface[vertexNum];
					// middle of edge1
					var edge1 = (cubeface[MyMath.Wrap(vertexNum - 1, 4)] + corner) / 2;
					// middle of edge2
					var edge2 = (cubeface[MyMath.Wrap(vertexNum + 1, 4)] + corner) / 2;

					// the cube vertex coordinates are 0.5 units, so multiply by 2
					corner *= 2;
					edge1 *= 2;
					edge2 *= 2;

					s_occlusionLookup[face, vertexNum] = new OcclusionLookupData()
					{
						Corner = corner.ToIntVector3(),
						Edge1 = edge1.ToIntVector3(),
						Edge2 = edge2.ToIntVector3(),
					};
				}
			}
		}
	}
}
