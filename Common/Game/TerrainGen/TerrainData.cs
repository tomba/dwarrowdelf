﻿using System;
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
		public TileData[, ,] TileGrid { get; private set; }
		public byte[,] LevelMap { get; private set; }

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public TerrainData(IntSize3 size)
		{
			this.Size = size;
			this.LevelMap = new byte[size.Height, size.Width];
			this.TileGrid = new TileData[size.Depth, size.Height, size.Width];
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		public int GetSurfaceLevel(int x, int y)
		{
			return this.LevelMap[y, x];
		}

		public int GetSurfaceLevel(IntPoint2 p)
		{
			return this.LevelMap[p.Y, p.X];
		}

		public IntPoint3 GetSurfaceLocation(int x, int y)
		{
			return new IntPoint3(x, y, GetSurfaceLevel(x, y));
		}

		public IntPoint3 GetSurfaceLocation(IntPoint2 p)
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

		public TileData GetTileData(IntPoint3 p)
		{
			return this.TileGrid[p.Z, p.Y, p.X];
		}

		public byte GetWaterLevel(IntPoint3 p)
		{
			return GetTileData(p).WaterLevel;
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			if (data.IsEmpty == false && this.LevelMap[p.Y, p.X] < p.Z)
			{
				Debug.Assert(p.Z >= 0 && p.Z < 256);
				this.LevelMap[p.Y, p.X] = (byte)p.Z;
			}
			else if (data.IsEmpty && this.LevelMap[p.Y, p.X] == p.Z)
			{
				if (p.Z == 0)
					throw new Exception();

				for (int z = p.Z - 1; z >= 0; --z)
				{
					if (GetTileData(new IntPoint3(p.X, p.Y, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						this.LevelMap[p.Y, p.X] = (byte)z;
						break;
					}
				}
			}

			SetTileDataNoHeight(p, data);
		}

		public void SetTileDataNoHeight(IntPoint3 p, TileData data)
		{
			this.TileGrid[p.Z, p.Y, p.X] = data;
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
							bw.Write(this.TileGrid[z, y, x].Raw);

				for (int y = 0; y < h; ++y)
					for (int x = 0; x < w; ++x)
						bw.Write(this.LevelMap[y, x]);
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

				var grid = terrain.TileGrid;
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

				var lm = terrain.LevelMap;
				for (int y = 0; y < h; ++y)
					for (int x = 0; x < w; ++x)
						lm[y, x] = br.ReadByte();

				return terrain;
			}
		}
	}
}
