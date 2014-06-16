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

namespace Client3D
{
	class Chunk : Component
	{
		public const int CHUNK_SIZE = 8;
		const int MAX_TILES = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
		const int MAX_VERTICES_PER_TILE = 36;
		const int FLOATS_PER_VERTEX = 5;

		static IntSize3 ChunkSize = new IntSize3(CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE);

		GameMap m_map;

		static VertexDataBuffer s_vertexData = new VertexDataBuffer(CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE * MAX_VERTICES_PER_TILE);

		Buffer<Vertex3003> m_vertexBuffer;
		VertexInputLayout m_layout;

		public IntVector3 ChunkOffset { get; private set; }

		public int VertexCount { get; private set; }
		public int ChunkRecalcs { get; private set; }

		// Maximum number of vertices this Chunk has had
		int m_maxVertices;

		bool m_chunkInvalid = true;

		public BoundingBox BBox;

		class VertexDataBuffer
		{
			public Vertex3003[] Data { get; private set; }
			public int Count { get; private set; }

			public VertexDataBuffer(int size)
			{
				this.Data = new Vertex3003[size];
				this.Count = 0;
			}

			public void Add(Vertex3003 data)
			{
				this.Data[this.Count++] = data;
			}

			public void Clear()
			{
				this.Count = 0;
			}
		}

		// Must match the texture array's index
		enum TextureID
		{
			TestTex = 0,
			Tex1,
			Tex2,
			Tex3,
			Tex4,
			Tex5,
			Undefined = 6,

			Grass = 1,
			Sand = 2,
		}

		public Chunk(GameMap map, IntVector3 chunkOffset)
		{
			this.ChunkOffset = chunkOffset;

			m_map = map;

			var v1 = chunkOffset.ToVector3();
			var v2 = v1 + new Vector3(Chunk.CHUNK_SIZE);
			this.BBox = new BoundingBox(v1, v2);
		}


		public void Setup(GraphicsDevice device)
		{
			m_vertexBuffer = Buffer.New<Vertex3003>(device, m_maxVertices, BufferFlags.VertexBuffer);
			m_layout = VertexInputLayout.FromBuffer(0, m_vertexBuffer);
		}

		public void InvalidateChunk()
		{
			m_chunkInvalid = true;
		}

		public void Free()
		{
			if (m_vertexBuffer != null)
			{
				//System.Diagnostics.Trace.TraceError("Free {0}", this.ChunkOffset);
				RemoveAndDispose(ref m_vertexBuffer);
				m_chunkInvalid = true;
			}
		}

		public void Render(TerrainRenderer scene)
		{
			var device = scene.Game.GraphicsDevice;

			if (m_chunkInvalid)
			{
				s_vertexData.Clear();

				GenerateVertices(s_vertexData, scene);

				if (s_vertexData.Count > 0)
				{
					if (m_vertexBuffer == null || m_vertexBuffer.ElementCount < s_vertexData.Count)
					{
						if (s_vertexData.Count > m_maxVertices)
							m_maxVertices = s_vertexData.Count;

						//System.Diagnostics.Trace.TraceError("Alloc {0}: {1} verts", this.ChunkOffset, m_maxVertices);

						Setup(device);
					}

					m_vertexBuffer.SetData(s_vertexData.Data, 0, s_vertexData.Count);
				}

				m_chunkInvalid = false;
				this.ChunkRecalcs = 1;
				this.VertexCount = s_vertexData.Count;
			}
			else
			{
				this.ChunkRecalcs = 0;
			}

			if (this.VertexCount > 0)
			{
				device.SetVertexBuffer(m_vertexBuffer);
				device.SetVertexInputLayout(m_layout);
				device.Draw(PrimitiveType.TriangleList, this.VertexCount);
			}
		}

		TextureID GetFloorTexture(TileData td)
		{
			switch (td.InteriorID)
			{
				case InteriorID.Grass:
				case InteriorID.Tree:
				case InteriorID.Shrub:
				case InteriorID.Sapling:
					return TextureID.Grass;

				case InteriorID.Empty:
					return TextureID.Sand;

				default:
					return TextureID.TestTex;
			}
		}

		void GenerateVertices(VertexDataBuffer vertexData, TerrainRenderer scene)
		{
			IntPoint3 cutn = scene.ViewCorner1;
			IntPoint3 cutp = scene.ViewCorner2;

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
						var p = new IntPoint3(x, y, z);
						var td = m_map.Grid[p.Z, p.Y, p.X];

						if (td.IsEmpty)
							continue;

						if (td.IsUndefined)
						{
							CreateCubicBlock(p, vertexData, scene, TextureID.Undefined);
							continue;
						}

						switch (td.InteriorID)
						{
							case InteriorID.NaturalWall:
								CreateCubicBlock(p, vertexData, scene, TextureID.Tex2);
								continue;
						}

						TextureID texID = GetFloorTexture(td);

						switch (td.TerrainID)
						{
							case TerrainID.NaturalFloor:
								CreateFloorBlock(p, vertexData, texID);
								continue;

							case TerrainID.Slope:
								CreateFloorBlock(p, vertexData, texID);
								continue;
						}
					}
				}
			}
		}

		void AddVertices(Vertex3002[] vertices, IntPoint3 p, TextureID texId, VertexDataBuffer vertexData)
		{
			var offset = p.ToVector3();
			offset += new Vector3(0.5f);

			for (int i = 0; i < vertices.Length; ++i)
			{
				var vd = new Vertex3003(vertices[i].Pos + offset, new Vector3(vertices[i].Tex, (int)texId));
				vertexData.Add(vd);
			}
		}

		// angle in 45 degree units
		void AddVertices(Vertex3002[] vertices, IntPoint3 p, TextureID texId, VertexDataBuffer vertexData, int angle)
		{
			var qua = Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.DegreesToRadians(angle * 45));

			var offset = p.ToVector3();
			offset += new Vector3(0.5f);

			for (int i = 0; i < vertices.Length; ++i)
			{
				var vd = new Vertex3003(vertices[i].Pos, new Vector3(vertices[i].Tex, (int)texId));
				vd.Position = Vector3.Transform(vd.Position, qua);
				vd.Position += offset;
				vertexData.Add(vd);
			}
		}

		void CreateFloorBlock(IntPoint3 p, VertexDataBuffer vertexData, TextureID texID)
		{
			AddVertices(s_floorCoords, p, texID, vertexData);
		}

		void CreateCubicBlock(IntPoint3 p, VertexDataBuffer vertexData, TerrainRenderer scene, TextureID texId)
		{
			/*
			 * 0 up
			 * 1 down
			 * 2 east
			 * 3 west
			 * 4 south
			 * 5 north
			 */

			int sides = 0;

			int x = p.X;
			int y = p.Y;
			int z = p.Z;

			var grid = m_map.Grid;

			// up
			if (z == scene.ViewCorner2.Z || (!grid[z + 1, y, x].IsUndefined && grid[z + 1, y, x].IsSeeThroughDown))
				sides |= 1 << (int)FaceDirection.PositiveZ;

			// down
			// Note: we never draw the bottommost layer in the map
			if (z != 0 && (!grid[z - 1, y, x].IsUndefined && grid[z - 1, y, x].IsSeeThrough))
				sides |= 1 << (int)FaceDirection.NegativeZ;

			// east
			if (x == scene.ViewCorner2.X || (!grid[z, y, x + 1].IsUndefined && grid[z, y, x + 1].IsSeeThrough))
				sides |= 1 << (int)FaceDirection.PositiveX;

			// west
			if (x == scene.ViewCorner1.X || (!grid[z, y, x - 1].IsUndefined && grid[z, y, x - 1].IsSeeThrough))
				sides |= 1 << (int)FaceDirection.NegativeX;

			// south
			if (y == scene.ViewCorner2.Y || (!grid[z, y + 1, x].IsUndefined && grid[z, y + 1, x].IsSeeThrough))
				sides |= 1 << (int)FaceDirection.PositiveY;

			// north
			if (y == scene.ViewCorner1.Y || (!grid[z, y - 1, x].IsUndefined && grid[z, y - 1, x].IsSeeThrough))
				sides |= 1 << (int)FaceDirection.NegativeY;

			if (sides == 0)
				return;

			CreateCube(p, vertexData, sides, texId);
		}

		void CreateCube(IntPoint3 p, VertexDataBuffer vertexData, int sides, TextureID texId)
		{
			for (int side = 0; side < 6 && sides != 0; ++side, sides >>= 1)
			{
				if ((sides & 1) == 0)
					continue;

				AddVertices(s_cubeFaces[side], p, texId, vertexData);
			}
		}

		static Vertex3002[][] s_cubeFaces;

		static Vertex3002[] s_floorCoords = new[]
		{
			new Vertex3002( 1.0f,   1.0f,  -1.0f,  1.0f,  1.0f),
			new Vertex3002(-1.0f,   1.0f,  -1.0f,  0.0f,  1.0f),
			new Vertex3002( 1.0f,  -1.0f,  -1.0f,  1.0f,  0.0f),
			new Vertex3002( 1.0f,  -1.0f,  -1.0f,  1.0f,  0.0f),
			new Vertex3002(-1.0f,   1.0f,  -1.0f,  0.0f,  1.0f),
			new Vertex3002(-1.0f,  -1.0f,  -1.0f,  0.0f,  0.0f),
		};

		static void HalvePositions(Vertex3002[] arr)
		{
			for (int i = 0; i < arr.Length; ++i)
				arr[i].Pos /= 2;
		}

		static Chunk()
		{
			CreateCubeFaces();

			HalvePositions(s_floorCoords);
		}

		static void CreateCubeFaces()
		{
			/*  south */
			var south = new Vertex3002[] {
				new Vertex3002(-1.0f,   1.0f,   1.0f,  0.0f,  0.0f),
				new Vertex3002( 1.0f,   1.0f,   1.0f,  1.0f,  0.0f),
				new Vertex3002(-1.0f,   1.0f,  -1.0f,  0.0f,  1.0f),
				new Vertex3002(-1.0f,   1.0f,  -1.0f,  0.0f,  1.0f),
				new Vertex3002( 1.0f,   1.0f,   1.0f,  1.0f,  0.0f),
				new Vertex3002( 1.0f,   1.0f,  -1.0f,  1.0f,  1.0f),
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

			s_cubeFaces = new Vertex3002[6][];

			for (int side = 0; side < 6; ++side)
			{
				Vertex3002[] face = new Vertex3002[6];

				for (int vn = 0; vn < 6; ++vn)
				{
					face[vn].Pos = Vector3.Transform(south[vn].Pos, rotQs[side]) / 2.0f;
					face[vn].Tex = south[vn].Tex;
				}

				s_cubeFaces[side] = face;
			}
		}

	}
}
