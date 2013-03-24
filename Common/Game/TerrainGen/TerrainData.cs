using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.TerrainGen
{
	public sealed class TerrainData
	{
		public IntSize3 Size { get; private set; }
		public TileData[, ,] TileGrid { get; private set; }
		public byte[,] HeightMap { get; private set; }

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public TerrainData(IntSize3 size)
		{
			this.Size = size;
			this.HeightMap = new byte[size.Height, size.Width];
			this.TileGrid = new TileData[size.Depth, size.Height, size.Width];
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		public int GetHeight(int x, int y)
		{
			return this.HeightMap[y, x];
		}

		public int GetHeight(IntPoint2 p)
		{
			return this.HeightMap[p.Y, p.X];
		}

		public TerrainID GetTerrainID(IntPoint3 p)
		{
			return this.TileGrid[p.Z, p.Y, p.X].TerrainID;
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 p)
		{
			return this.TileGrid[p.Z, p.Y, p.X].TerrainMaterialID;
		}

		public InteriorID GetInteriorID(IntPoint3 p)
		{
			return this.TileGrid[p.Z, p.Y, p.X].InteriorID;
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 p)
		{
			return this.TileGrid[p.Z, p.Y, p.X].InteriorMaterialID;
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
			return this.TileGrid[p.Z, p.Y, p.X].WaterLevel;
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			if (data.IsEmpty == false && this.HeightMap[p.Y, p.X] < p.Z)
			{
				Debug.Assert(p.Z >= 0 && p.Z < 256);
				this.HeightMap[p.Y, p.X] = (byte)p.Z;
			}
			else if (data.IsEmpty && this.HeightMap[p.Y, p.X] == p.Z)
			{
				if (p.Z == 0)
					throw new Exception();

				for (int z = p.Z - 1; z >= 0; --z)
				{
					if (GetTileData(new IntPoint3(p.X, p.Y, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						this.HeightMap[p.Y, p.X] = (byte)z;
						break;
					}
				}
			}

			this.TileGrid[p.Z, p.Y, p.X] = data;
		}

		public void SetTileDataNoHeight(IntPoint3 p, TileData data)
		{
			this.TileGrid[p.Z, p.Y, p.X] = data;
		}
	}
}
