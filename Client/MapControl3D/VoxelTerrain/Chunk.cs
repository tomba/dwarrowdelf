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

namespace Dwarrowdelf.Client
{
	class Chunk
	{
		public const int CHUNK_SIZE = 16;
		public const int VOXELS_PER_CHUNK = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
		public const int MAX_VERTICES_PER_CHUNK = VOXELS_PER_CHUNK * 6;

		public static readonly IntSize3 ChunkSize = new IntSize3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE);

		EnvironmentObject m_map;
		VoxelMap m_voxelMap;

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

		Buffer<TerrainVertex> m_vertexBuffer;
		public int VertexCount { get; private set; }

		Buffer<SceneryVertex> m_sceneryVertexBuffer;
		public int SceneryVertexCount { get; private set; }

		public BoundingBox BBox;

		public Chunk(EnvironmentObject map, IntVector3 chunkPosition)
		{
			this.ChunkPosition = chunkPosition;
			this.ChunkOffset = chunkPosition * CHUNK_SIZE;

			m_map = map;

			var v1 = this.ChunkOffset.ToVector3();
			var v2 = v1 + new Vector3(Chunk.CHUNK_SIZE);
			this.BBox = new BoundingBox(v1, v2);
		}

		bool m_scanned;
		public bool IsAllEmpty { get; private set; }
		public bool IsAllUndefined { get; private set; }

		void ScanForAllEmptyOrUndefined()
		{
			m_scanned = true;

			TileData first = m_map.GetTileData(this.ChunkOffset);

			foreach (var p in Chunk.ChunkSize.Range())
			{
				var mp = this.ChunkOffset + p;

				var td = m_map.GetTileData(mp);

				if (td.Raw != first.Raw)
				{
					this.IsAllEmpty = false;
					this.IsAllUndefined = false;
					return;
				}
			}

			this.IsAllEmpty = first.IsEmpty;
			this.IsAllUndefined = first.IsUndefined;
		}

		void FillVoxelMap()
		{
			m_voxelMap = new VoxelMap(ChunkSize);

			foreach (var p in m_voxelMap.Size.Range())
			{
				var mp = this.ChunkOffset + p;

				var td = m_map.GetTileData(mp);

				// we don't use VisibleFaces for Empty, and Undefined is always hidden
				if (td.IsEmptyNoWater || td.IsUndefined)
					continue;

				Voxel v = new Voxel();

				v.VisibleFaces = GetVisibleFaces(mp);

				m_voxelMap.SetVoxel(mp - this.ChunkOffset, v);
			}
		}

		public void UpdateVoxel(IntVector3 mp)
		{
			if (this.IsAllEmpty || this.IsAllUndefined)
			{
				// presume the chunk is no longer empty or undefined
				this.IsAllEmpty = this.IsAllUndefined = false;
				return;
			}

			if (m_voxelMap == null)
				return;

			var td = m_map.GetTileData(mp);

			Voxel v = new Voxel();

			// we don't use VisibleFaces for Empty, and Undefined is always hidden
			if (!td.IsEmptyNoWater && !td.IsUndefined)
				v.VisibleFaces = GetVisibleFaces(mp);

			m_voxelMap.SetVoxel(mp - this.ChunkOffset, v);
		}

		Direction GetVisibleFaces(IntVector3 p)
		{
			Direction visibleFaces = 0;

			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var n = p + dir;

				if (m_map.Size.Contains(n) == false)
					continue;

				var td = m_map.GetTileData(n);

				if (td.IsUndefined || td.IsSeeThrough == false)
					continue;

				visibleFaces |= dir;
			}

			return visibleFaces;
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

			Direction visibleChunkFaces = 0;
			if (diff.X >= 0)
				visibleChunkFaces |= Direction.PositiveX;
			if (diff.X <= 0)
				visibleChunkFaces |= Direction.NegativeX;
			if (diff.Y >= 0)
				visibleChunkFaces |= Direction.PositiveY;
			if (diff.Y <= 0)
				visibleChunkFaces |= Direction.NegativeY;
			if (diff.Z >= 0)
				visibleChunkFaces |= Direction.PositiveZ;
			if (diff.Z <= 0)
				visibleChunkFaces |= Direction.NegativeZ;

			GenerateVertices(ref viewGrid, visibleChunkFaces, terrainVertexList, sceneryVertexList);

			this.VertexCount = terrainVertexList.Count;
			this.SceneryVertexCount = sceneryVertexList.Count;
		}

		void GenerateVertices(ref IntGrid3 viewGrid, Direction visibleChunkFaces,
			VertexList<TerrainVertex> terrainVertexList,
			VertexList<SceneryVertex> sceneryVertexList)
		{
			IntGrid3 chunkGrid = viewGrid.Intersect(new IntGrid3(this.ChunkOffset, Chunk.ChunkSize));

			// is the chunk inside frustum, but outside the viewgrid?
			if (chunkGrid.IsNull)
				return;

			if (m_scanned == false)
				ScanForAllEmptyOrUndefined();

			if (this.IsAllEmpty)
				return;

			if (this.IsAllUndefined)
			{
				CreateUndefinedChunk(ref viewGrid, ref chunkGrid, terrainVertexList, visibleChunkFaces);
				return;
			}

			if (m_voxelMap == null)
				FillVoxelMap();

			// Draw from up to down to avoid overdraw
			for (int z = chunkGrid.Z2; z >= chunkGrid.Z1; --z)
			{
				for (int y = chunkGrid.Y1; y <= chunkGrid.Y2; ++y)
				{
					for (int x = chunkGrid.X1; x <= chunkGrid.X2; ++x)
					{
						var p = new IntVector3(x, y, z);

						var td = m_map.GetTileData(p);

						if (td.WaterLevel == 0)
						{
							if (td.IsEmpty)
								continue;
						}

						var pos = p - this.ChunkOffset;

						if (td.HasTree)
						{
							// Add tree as scenery vertex
							HandleTree(sceneryVertexList, td, ref pos);
							continue;
						}

						if (td.IsGreen) // XXX
							continue;

						var vox = m_voxelMap.Grid[pos.Z, pos.Y, pos.X];

						HandleVoxel(p, ref vox, ref viewGrid, visibleChunkFaces, terrainVertexList);
					}
				}
			}
		}

		static void HandleTree(VertexList<SceneryVertex> sceneryVertexList, TileData td, ref IntVector3 pos)
		{
			SymbolID symbol;
			Color color;

			switch (td.ID)
			{
				case TileID.Tree:
					switch (td.MaterialID)
					{
						case MaterialID.Fir:
							symbol = SymbolID.ConiferousTree;
							break;

						case MaterialID.Pine:
							symbol = SymbolID.ConiferousTree2;
							break;

						case MaterialID.Birch:
							symbol = SymbolID.DeciduousTree;
							break;

						case MaterialID.Oak:
							symbol = SymbolID.DeciduousTree2;
							break;

						default:
							throw new Exception();
					}
					break;

				case TileID.Sapling:
					switch (td.MaterialID)
					{
						case MaterialID.Fir:
							symbol = SymbolID.ConiferousSapling;
							break;

						case MaterialID.Pine:
							symbol = SymbolID.ConiferousSapling2;
							break;

						case MaterialID.Birch:
							symbol = SymbolID.DeciduousSapling;
							break;

						case MaterialID.Oak:
							symbol = SymbolID.DeciduousSapling2;
							break;

						default:
							throw new Exception();
					}
					break;

				case TileID.DeadTree:
					symbol = SymbolID.DeadTree;
					break;

				default:
					throw new Exception();
			}

			color = Color.ForestGreen;

			sceneryVertexList.Add(new SceneryVertex(pos.ToVector3(), Color.LightGreen, (uint)symbol));
		}

		void CreateUndefinedChunk(ref IntGrid3 viewGrid, ref IntGrid3 chunkGrid, VertexList<TerrainVertex> vertexList,
			Direction visibleChunkFaces)
		{
			// clear the visible chunk faces that are not at the view's edge

			// up
			if ((visibleChunkFaces & Direction.PositiveZ) != 0 && chunkGrid.Z2 != viewGrid.Z2)
				visibleChunkFaces &= ~Direction.PositiveZ;

			// down
			// Note: we never draw the bottommost layer in the map
			visibleChunkFaces &= ~Direction.NegativeZ;

			// east
			if ((visibleChunkFaces & Direction.PositiveX) != 0 && chunkGrid.X2 != viewGrid.X2)
				visibleChunkFaces &= ~Direction.PositiveX;

			// west
			if ((visibleChunkFaces & Direction.NegativeX) != 0 && chunkGrid.X1 != viewGrid.X1)
				visibleChunkFaces &= ~Direction.NegativeX;

			// south
			if ((visibleChunkFaces & Direction.PositiveY) != 0 && chunkGrid.Y2 != viewGrid.Y2)
				visibleChunkFaces &= ~Direction.PositiveY;

			// north
			if ((visibleChunkFaces & Direction.NegativeY) != 0 && chunkGrid.Y1 != viewGrid.Y1)
				visibleChunkFaces &= ~Direction.NegativeY;

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

				bool posFace = (side & 1) == 1;

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

		void GetTextures(IntVector3 p, ref Voxel vox, out FaceTexture baseTexture, out FaceTexture topTexture)
		{
			var td = m_map.GetTileData(p);

			baseTexture = new FaceTexture();
			topTexture = new FaceTexture();

			if (td.IsUndefined)
			{
				baseTexture.Symbol1 = SymbolID.Unknown;
				baseTexture.Color1 = GameColor.LightGray;
				return;
			}

			if (td.WaterLevel > 0)
			{
				baseTexture.Symbol1 = SymbolID.Water;
				baseTexture.Color0 = GameColor.MediumBlue;
				baseTexture.Color1 = GameColor.SeaGreen;
				topTexture = baseTexture;
				return;
			}

			switch (td.ID)
			{
				case TileID.Undefined:
					baseTexture.Symbol1 = SymbolID.Unknown;
					baseTexture.Color1 = GameColor.LightGray;
					return;

				case TileID.Empty:
					throw new Exception();

				case TileID.NaturalWall:
					var matInfo = Materials.GetMaterial(td.MaterialID);
					var color = matInfo.Color;

					baseTexture.Color0 = GameColor.None;
					baseTexture.Symbol1 = SymbolID.Wall;
					baseTexture.Color1 = color;

					var secondaryMatInfo = Materials.GetMaterial(td.SecondaryMaterialID);

					switch (secondaryMatInfo.Category)
					{
						case MaterialCategory.Gem:
							baseTexture.Symbol2 = SymbolID.GemOre;
							baseTexture.Color2 = secondaryMatInfo.Color;
							break;

						case MaterialCategory.Mineral:
							baseTexture.Symbol2 = SymbolID.ValuableOre;
							baseTexture.Color2 = secondaryMatInfo.Color;
							break;

						default:
							break;
					}

					// If the top face of the tile is visible, we have a "floor"
					if ((vox.VisibleFaces & Direction.PositiveZ) != 0)
					{
						if (m_map.Contains(p.Up) && m_map.GetTileData(p.Up).IsGreen)
						{
							var tdUp = m_map.GetTileData(p.Up);
							var matInfoUp = Materials.GetMaterial(tdUp.MaterialID);

							SymbolID symbol;

							switch (matInfoUp.ID)
							{
								case MaterialID.ReedGrass:
									symbol = SymbolID.Grass4;
									break;

								case MaterialID.RyeGrass:
									symbol = SymbolID.Grass2;
									break;

								case MaterialID.MeadowGrass:
									symbol = SymbolID.Grass3;
									break;

								case MaterialID.HairGrass:
									symbol = SymbolID.Grass;
									break;

								default:
									symbol = SymbolID.Undefined;
									break;
							}

							topTexture.Color0 = GameColor.Green;
							topTexture.Symbol1 = symbol;
							topTexture.Color1 = GameColor.Green;
						}
						else if (matInfo.Category == MaterialCategory.Soil)
						{
							topTexture.Color0 = color;
							topTexture.Symbol1 = SymbolID.Sand;
							topTexture.Color1 = color;
						}
						else
						{
							topTexture.Color0 = color;
							topTexture.Symbol1 = SymbolID.Floor;
							topTexture.Color1 = color;
						}
						//floorTile.BgColor = GetTerrainBackgroundColor(matInfoDown);
					}
					else
					{
						topTexture = baseTexture;
					}
					return;

				case TileID.Stairs:
					baseTexture.Symbol1 = SymbolID.StairsDown;
					baseTexture.Color1 = GameColor.Red;
					topTexture = baseTexture;
					return;

				default:
					throw new Exception();
			}
		}

		void HandleVoxel(IntVector3 p, ref Voxel vox, ref IntGrid3 viewGrid, Direction visibleChunkFaces,
			VertexList<TerrainVertex> vertexList)
		{
			FaceTexture baseTexture, topTexture;

			GetTextures(p, ref vox, out baseTexture, out topTexture);

			int x = p.X;
			int y = p.Y;
			int z = p.Z;

			Direction visibleFaces = visibleChunkFaces & vox.VisibleFaces;
			/* sides that are shown due to the viewgrid, but are really hidden by other voxels */
			Direction visibleHiddenFaces = 0;

			// up
			if ((visibleChunkFaces & Direction.PositiveZ) != 0 && z == viewGrid.Z2)
			{
				const Direction b = Direction.PositiveZ;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
				// override the top tex to remove the grass
				topTexture = baseTexture;
			}

			// down
			// Note: we never draw the bottommost layer in the map
			if (z == 0)
				visibleFaces &= ~Direction.NegativeZ;

			// east
			if ((visibleChunkFaces & Direction.PositiveX) != 0 && x == viewGrid.X2)
			{
				const Direction b = Direction.PositiveX;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// west
			if ((visibleChunkFaces & Direction.NegativeX) != 0 && x == viewGrid.X1)
			{
				const Direction b = Direction.NegativeX;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// south
			if ((visibleChunkFaces & Direction.PositiveY) != 0 && y == viewGrid.Y2)
			{
				const Direction b = Direction.PositiveY;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			// north
			if ((visibleChunkFaces & Direction.NegativeY) != 0 && y == viewGrid.Y1)
			{
				const Direction b = Direction.NegativeY;
				visibleHiddenFaces |= b & ~visibleFaces;
				visibleFaces |= b;
			}

			if (visibleFaces == 0)
				return;

			CreateCube(p, visibleFaces, visibleHiddenFaces, ref baseTexture, ref topTexture, vertexList);
		}

		void CreateCube(IntVector3 p, Direction visibleFaces, Direction visibleHiddenFaces,
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
					GetOcclusionsForFace(p, (DirectionOrdinal)side,
						out occ0, out occ1, out occ2, out occ3);
				}

				var vd = new TerrainVertex(v0, v1, v2, v3, occ0, occ1, occ2, occ3,
					side == (int)DirectionOrdinal.PositiveZ ? topTexture : baseTexture);
				vertexList.Add(vd);
			}
		}

		bool IsBlocker(IntVector3 p)
		{
			if (m_map.Size.Contains(p) == false)
				return false;

			var td = m_map.GetTileData(p);

			return td.IsUndefined || td.IsSeeThrough == false;
		}

		void GetOcclusionsForFace(IntVector3 p, DirectionOrdinal face,
			out int o0, out int o1, out int o2, out int o3)
		{
			// XXX can we store occlusion data to the Voxel?

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
			var normal = normalDir.ToIntVector3();
			var up = upDir.ToIntVector3();
			var right = rightDir.ToIntVector3();

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
		/// Cube face infos, in the same order as DirectionOrdinal enum
		/// </summary>
		public static readonly CubeFaceInfo[] s_cubeFaceInfo;

		static Chunk()
		{
			s_cubeFaceInfo = new CubeFaceInfo[6];
			s_cubeFaceInfo[(int)DirectionOrdinal.West] = CreateFaceInfo(Direction.West, Direction.Up, Direction.South);
			s_cubeFaceInfo[(int)DirectionOrdinal.East] = CreateFaceInfo(Direction.East, Direction.Up, Direction.North);
			s_cubeFaceInfo[(int)DirectionOrdinal.North] = CreateFaceInfo(Direction.North, Direction.Up, Direction.West);
			s_cubeFaceInfo[(int)DirectionOrdinal.South] = CreateFaceInfo(Direction.South, Direction.Up, Direction.East);
			s_cubeFaceInfo[(int)DirectionOrdinal.Down] = CreateFaceInfo(Direction.Down, Direction.North, Direction.West);
			s_cubeFaceInfo[(int)DirectionOrdinal.Up] = CreateFaceInfo(Direction.Up, Direction.North, Direction.East);
		}

		public class CubeFaceInfo
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
