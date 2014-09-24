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

		static readonly IntSize3 ChunkSize = new IntSize3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE);

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
		/// Chunk contains only Empty voxels without trees etc.
		/// </summary>
		public bool IsEmpty { get; set; }

		/// <summary>
		/// The chunk contains only undefined voxels
		/// </summary>
		public bool IsUndefined { get; set; }

		Buffer<TerrainVertex> m_vertexBuffer;
		public int VertexCount { get; private set; }

		Buffer<SceneryVertex> m_sceneryVertexBuffer;
		public int SceneryVertexCount { get; private set; }

		public BoundingBox BBox;

		public Chunk(VoxelMap map, IntVector3 chunkPosition)
		{
			this.ChunkPosition = chunkPosition;
			this.ChunkOffset = chunkPosition * CHUNK_SIZE;

			m_map = map;

			var v1 = this.ChunkOffset.ToVector3();
			var v2 = v1 + new Vector3(Chunk.CHUNK_SIZE);
			this.BBox = new BoundingBox(v1, v2);

			CheckIfEmptyOrUndefined();
		}

		void CheckIfEmptyOrUndefined()
		{
			int x0 = this.ChunkOffset.X;
			int x1 = this.ChunkOffset.X + CHUNK_SIZE - 1;

			int y0 = this.ChunkOffset.Y;
			int y1 = this.ChunkOffset.Y + CHUNK_SIZE - 1;

			int z0 = this.ChunkOffset.Z;
			int z1 = this.ChunkOffset.Z + CHUNK_SIZE - 1;

			uint current = m_map.Grid[z0, y0, x0].Raw;

			for (int z = z0; z <= z1; ++z)
			{
				for (int y = y0; y <= y1; ++y)
				{
					for (int x = x0; x <= x1; ++x)
					{
						if (current != m_map.Grid[z, y, x].Raw)
						{
							this.IsUndefined = false;
							this.IsEmpty = false;
							return;
						}
					}
				}
			}

			Voxel vox = new Voxel() { Raw = current };

			if (vox.IsUndefined)
				this.IsUndefined = true;
			else if (vox.IsEmpty)
				this.IsEmpty = true;
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

		public void GenerateVertices(TerrainRenderer scene, IntVector3 cameraChunkPos,
			VertexList<TerrainVertex> terrainVertexList, VertexList<SceneryVertex> sceneryVertexList)
		{
			terrainVertexList.Clear();
			sceneryVertexList.Clear();

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

			GenerateVertices(scene, mask, terrainVertexList, sceneryVertexList);

			this.VertexCount = terrainVertexList.Count;
			this.SceneryVertexCount = sceneryVertexList.Count;
		}

		void GenerateVertices(TerrainRenderer scene, FaceDirectionBits mask, VertexList<TerrainVertex> terrainVertexList,
			VertexList<SceneryVertex> sceneryVertexList)
		{
			IntGrid3 grid = new IntGrid3(this.ChunkOffset, Chunk.ChunkSize);
			grid = scene.ViewGrid.Intersect(grid);

			if (grid.IsNull)
				return;

			// Draw from up to down to avoid overdraw
			for (int z = grid.Z2; z >= grid.Z1; --z)
			{
				for (int y = grid.Y1; y <= grid.Y2; ++y)
				{
					for (int x = grid.X1; x <= grid.X2; ++x)
					{
						var td = m_map.Grid[z, y, x];

						if (td.IsEmpty)
							continue;

						var p = new IntVector3(x, y, z);

						if ((td.Flags & VoxelFlags.Tree) != 0)
						{
							var pos = p - this.ChunkOffset;
							sceneryVertexList.Add(new SceneryVertex(pos.ToVector3(), Color.LightGreen,
								(int)Dwarrowdelf.Client.SymbolID.ConiferousTree));

							if (td.Type != VoxelType.Empty)
								throw new Exception();

							continue;
						}

						if (td.Type == VoxelType.Empty)
							continue;

						FaceTexture baseTexture = new FaceTexture();
						FaceTexture topTexture = new FaceTexture();

						switch (td.Type)
						{
							case VoxelType.Undefined:
								baseTexture.Symbol1 = SymbolID.Unknown;
								baseTexture.Color1 = GameColor.LightGray;
								break;

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

								if ((td.Flags & VoxelFlags.Grass) != 0)
								{
									topTexture.Color0 = GameColor.LightGreen;
									topTexture.Symbol1 = SymbolID.Grass;
									topTexture.Color1 = GameColor.LightGreen;
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
								break;
						}

						CreateCubicBlock(p, scene, baseTexture, topTexture, mask, terrainVertexList);
					}
				}
			}
		}

		void CreateCubicBlock(IntVector3 p, TerrainRenderer scene,
			FaceTexture baseTexture, FaceTexture topTexture,
			FaceDirectionBits globalFaceMask, VertexList<TerrainVertex> vertexList)
		{
			int x = p.X;
			int y = p.Y;
			int z = p.Z;

			var vd = m_map.Grid[z, y, x];

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

			CreateCube(p, sides, hiddenSides, baseTexture, topTexture, vertexList);
		}

		bool IsBlocker(IntVector3 p)
		{
			if (m_map.Size.Contains(p) == false)
				return false;

			return m_map.Grid[p.Z, p.Y, p.X].IsOpaque;
		}

		void CreateCube(IntVector3 p, FaceDirectionBits faceMask, FaceDirectionBits hiddenFaceMask,
			FaceTexture baseTexture, FaceTexture topTexture, VertexList<TerrainVertex> vertexList)
		{
			var offset = p - this.ChunkOffset;

			int sides = (int)faceMask;

			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				var vertices = s_intCubeFaces[side];

				int occ0, occ1, occ2, occ3;

				if (((int)hiddenFaceMask & (1 << side)) != 0)
				{
					occ0 = occ1 = occ2 = occ3 = 4;
				}
				else
				{
					occ0 = GetOcclusionForFaceVertex(p, (FaceDirection)side, s_cubeIndices[0]);
					occ1 = GetOcclusionForFaceVertex(p, (FaceDirection)side, s_cubeIndices[1]);
					occ2 = GetOcclusionForFaceVertex(p, (FaceDirection)side, s_cubeIndices[2]);
					occ3 = GetOcclusionForFaceVertex(p, (FaceDirection)side, s_cubeIndices[3]);
				}

				IntVector3 v0, v1, v2, v3;

				v0 = vertices[s_cubeIndices[0]] + offset;
				v1 = vertices[s_cubeIndices[1]] + offset;
				v2 = vertices[s_cubeIndices[2]] + offset;
				v3 = vertices[s_cubeIndices[3]] + offset;

				var vd = new TerrainVertex(v0, v1, v2, v3, occ0, occ1, occ2, occ3,
					side == (int)FaceDirection.PositiveZ ? topTexture : baseTexture);
				vertexList.Add(vd);
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
