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

		public bool Contains(IntPoint3 p)
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

		public IntPoint3 GetSurfaceLocation(int x, int y)
		{
			return new IntPoint3(x, y, GetSurfaceLevel(x, y));
		}

		public IntPoint3 GetSurfaceLocation(IntVector2 p)
		{
			return new IntPoint3(p, GetSurfaceLevel(p));
		}

		public TerrainID GetTerrainID(IntPoint3 p)
		{
			return GetTileData(p).TerrainID;
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 p)
		{
			return GetTileData(p).TerrainMaterialID;
		}

		public InteriorID GetInteriorID(IntPoint3 p)
		{
			return GetTileData(p).InteriorID;
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 p)
		{
			return GetTileData(p).InteriorMaterialID;
		}

		public TerrainInfo GetTerrain(IntPoint3 p)
		{
			return Terrains.GetTerrain(GetTerrainID(p));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3 p)
		{
			return Materials.GetMaterial(GetTerrainMaterialID(p));
		}

		public InteriorInfo GetInterior(IntPoint3 p)
		{
			return Interiors.GetInterior(GetInteriorID(p));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3 p)
		{
			return Materials.GetMaterial(GetInteriorMaterialID(p));
		}

		public TileData GetTileData(int x, int y, int z)
		{
			return m_tileGrid[z, y, x];
		}

		public TileData GetTileData(IntPoint3 p)
		{
			return m_tileGrid[p.Z, p.Y, p.X];
		}

		public byte GetWaterLevel(IntPoint3 p)
		{
			return GetTileData(p).WaterLevel;
		}

		public void SetTileData(IntPoint3 p, TileData data)
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
					if (GetTileData(new IntPoint3(p.X, p.Y, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						SetSurfaceLevel(p.X, p.Y, z);
						break;
					}
				}
			}

			SetTileDataNoHeight(p, data);
		}

		public void SetTileDataNoHeight(IntPoint3 p, TileData data)
		{
			m_tileGrid[p.Z, p.Y, p.X] = data;
		}

		public void SaveTerrain(string path)
		{
			using (var stream = File.Create(path))
			using (var bw = new BinaryWriter(stream))
			{
				bw.Write(this.Size.Width);
				bw.Write(this.Size.Height);
				bw.Write(this.Size.Depth);

				int w = this.Width;
				int h = this.Height;
				int d = this.Depth;

				for (int z = 0; z < d; ++z)
					for (int y = 0; y < h; ++y)
						for (int x = 0; x < w; ++x)
							bw.Write(GetTileData(x, y, z).Raw);

				for (int y = 0; y < h; ++y)
					for (int x = 0; x < w; ++x)
						bw.Write((byte)GetSurfaceLevel(x, y));
			}
		}

		public static TerrainData LoadTerrain(string path, IntSize3 expectedSize)
		{
			using (var stream = File.OpenRead(path))
			using (var br = new BinaryReader(stream))
			{
				int w = br.ReadInt32();
				int h = br.ReadInt32();
				int d = br.ReadInt32();

				var size = new IntSize3(w, h, d);

				if (size != expectedSize)
					return null;

				var terrain = new TerrainData(size);

				var grid = terrain.m_tileGrid;
				for (int z = 0; z < d; ++z)
				{
					for (int y = 0; y < h; ++y)
					{
						for (int x = 0; x < w; ++x)
						{
							grid[z, y, x].Raw = br.ReadUInt64();
						}
					}
				}

				var lm = terrain.m_levelMap;
				for (int y = 0; y < h; ++y)
					for (int x = 0; x < w; ++x)
						lm[y, x] = br.ReadByte();

				return terrain;
			}
		}
	}
}
