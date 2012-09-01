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
		public TileGrid TileGrid { get; private set; }
		public byte[,] HeightMap { get; private set; }

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public TerrainData(IntSize3 size)
		{
			this.Size = size;
			this.HeightMap = new byte[size.Height, size.Width];
			this.TileGrid = new TileGrid(size);
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

		public TerrainID GetTerrainID(IntPoint3 l)
		{
			return this.TileGrid.GetTerrainID(l);
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 l)
		{
			return this.TileGrid.GetTerrainMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3 l)
		{
			return this.TileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 l)
		{
			return this.TileGrid.GetInteriorMaterialID(l);
		}

		public TerrainInfo GetTerrain(IntPoint3 l)
		{
			return Terrains.GetTerrain(GetTerrainID(l));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(this.TileGrid.GetTerrainMaterialID(l));
		}

		public InteriorInfo GetInterior(IntPoint3 l)
		{
			return Interiors.GetInterior(GetInteriorID(l));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(this.TileGrid.GetInteriorMaterialID(l));
		}

		public TileData GetTileData(IntPoint3 l)
		{
			return this.TileGrid.GetTileData(l);
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return this.TileGrid.GetWaterLevel(l);
		}

		public bool GetTileFlag(IntPoint3 l, TileFlags flag)
		{
			return (this.TileGrid.GetFlags(l) & flag) != 0;
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
					if (this.TileGrid.GetTileData(new IntPoint3(p.X, p.Y, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						this.HeightMap[p.Y, p.X] = (byte)z;
						break;
					}
				}
			}

			this.TileGrid.SetTileData(p, data);
		}

		public void SetTileFlags(IntPoint3 l, TileFlags flags, bool value)
		{
			if (value)
				this.TileGrid.SetFlags(l, flags);
			else
				this.TileGrid.ClearFlags(l, flags);
		}
	}
}
