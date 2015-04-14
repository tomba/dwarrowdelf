using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.TerrainGen
{
	public sealed class TerrainData
	{
		public IntSize3 Size { get; private set; }
		TileData[, ,] m_tileGrid;
		byte[,] m_levelMap;

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public TerrainData(IntSize3 size)
		{
			this.Size = size;
			m_levelMap = new byte[size.Height, size.Width];
			m_tileGrid = new TileData[size.Depth, size.Height, size.Width];
		}

		public void GetData(out TileData[, ,] tileGrid, out byte[,] levelMap)
		{
			tileGrid = m_tileGrid;
			levelMap = m_levelMap;
		}

		public bool Contains(IntVector3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		public int GetSurfaceLevel(int x, int y)
		{
			return m_levelMap[y, x];
		}

		public int GetSurfaceLevel(IntVector2 p)
		{
			return m_levelMap[p.Y, p.X];
		}

		public void SetSurfaceLevel(int x, int y, int level)
		{
			m_levelMap[y, x] = (byte)level;
		}

		public IntVector3 GetSurfaceLocation(int x, int y)
		{
			return new IntVector3(x, y, GetSurfaceLevel(x, y));
		}

		public IntVector3 GetSurfaceLocation(IntVector2 p)
		{
			return new IntVector3(p, GetSurfaceLevel(p));
		}

		public TerrainID GetTerrainID(IntVector3 p)
		{
			return GetTileData(p).TerrainID;
		}

		public MaterialID GetTerrainMaterialID(IntVector3 p)
		{
			return GetTileData(p).TerrainMaterialID;
		}

		public InteriorID GetInteriorID(IntVector3 p)
		{
			return GetTileData(p).InteriorID;
		}

		public MaterialID GetInteriorMaterialID(IntVector3 p)
		{
			return GetTileData(p).InteriorMaterialID;
		}

		public MaterialInfo GetTerrainMaterial(IntVector3 p)
		{
			return Materials.GetMaterial(GetTerrainMaterialID(p));
		}

		public MaterialInfo GetInteriorMaterial(IntVector3 p)
		{
			return Materials.GetMaterial(GetInteriorMaterialID(p));
		}

		public TileData GetTileData(int x, int y, int z)
		{
			return m_tileGrid[z, y, x];
		}

		public TileData GetTileData(IntVector3 p)
		{
			return m_tileGrid[p.Z, p.Y, p.X];
		}

		public byte GetWaterLevel(IntVector3 p)
		{
			return GetTileData(p).WaterLevel;
		}

		public void SetTileData(IntVector3 p, TileData data)
		{
			int oldLevel = GetSurfaceLevel(p.X, p.Y);

			if (data.IsEmpty == false && oldLevel < p.Z)
			{
				Debug.Assert(p.Z >= 0 && p.Z < 256);
				SetSurfaceLevel(p.X, p.Y, p.Z);
			}
			else if (data.IsEmpty && oldLevel == p.Z)
			{
				if (p.Z == 0)
					throw new Exception();

				for (int z = p.Z - 1; z >= 0; --z)
				{
					if (GetTileData(new IntVector3(p.X, p.Y, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						SetSurfaceLevel(p.X, p.Y, z);
						break;
					}
				}
			}

			SetTileDataNoHeight(p, data);
		}

		public void SetTileDataNoHeight(IntVector3 p, TileData data)
		{
			m_tileGrid[p.Z, p.Y, p.X] = data;
		}

		public unsafe void SaveTerrain(string path)
		{
			using (var stream = File.Create(path))
			{
				using (var bw = new BinaryWriter(stream, Encoding.Default, true))
				{
					bw.Write(this.Size.Width);
					bw.Write(this.Size.Height);
					bw.Write(this.Size.Depth);
				}

				fixed (TileData* v = this.m_tileGrid)
				{
					byte* p = (byte*)v;
					using (var memStream = new UnmanagedMemoryStream(p, this.Size.Volume * sizeof(TileData)))
						memStream.CopyTo(stream);
				}

				fixed (byte* v = this.m_levelMap)
				{
					byte* p = (byte*)v;
					using (var memStream = new UnmanagedMemoryStream(p, this.Width * this.Height * sizeof(byte)))
						memStream.CopyTo(stream);
				}
			}
		}

		public unsafe static TerrainData LoadTerrain(string path, IntSize3 expectedSize)
		{
			using (var stream = File.OpenRead(path))
			{
				TerrainData terrain;

				using (var br = new BinaryReader(stream, Encoding.Default, true))
				{
					int w = br.ReadInt32();
					int h = br.ReadInt32();
					int d = br.ReadInt32();

					var size = new IntSize3(w, h, d);

					if (size != expectedSize)
						return null;

					terrain = new TerrainData(size);
				}

				fixed (TileData* v = terrain.m_tileGrid)
				{
					byte* p = (byte*)v;

					int len = terrain.Size.Volume * sizeof(TileData);

					using (var memStream = new UnmanagedMemoryStream(p, 0, len, FileAccess.Write))
						CopyTo(stream, memStream, len);
				}

				fixed (byte* p = terrain.m_levelMap)
				{
					int len = terrain.Size.Plane.Area * sizeof(byte);

					using (var memStream = new UnmanagedMemoryStream(p, 0, len, FileAccess.Write))
						CopyTo(stream, memStream, len);
				}

				return terrain;
			}
		}

		static void CopyTo(Stream input, Stream output, int len)
		{
			var arr = new byte[4096 * 8];

			while (len > 0)
			{
				int l = input.Read(arr, 0, Math.Min(len, arr.Length));

				if (l == 0)
					throw new EndOfStreamException();

				output.Write(arr, 0, l);

				len -= l;
			}
		}
	}
}
