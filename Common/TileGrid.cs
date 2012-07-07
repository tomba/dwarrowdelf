using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	sealed public class TileGrid
	{
		public TileData[, ,] Grid { get; private set; }
		public IntSize3 Size { get; private set; }
		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public TileGrid(IntSize3 size)
		{
			this.Size = size;
			this.Grid = new TileData[size.Depth, size.Height, size.Width];
		}

		public bool Contains(IntPoint3 p)
		{
			return this.Size.Contains(p);
		}

		public TileData GetTileData(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X];
		}

		public TerrainID GetTerrainID(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].TerrainID;
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].TerrainMaterialID;
		}

		public InteriorID GetInteriorID(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].InteriorID;
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].InteriorMaterialID;
		}

		public byte GetWaterLevel(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].WaterLevel;
		}

		public TileFlags GetFlags(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].Flags;
		}


		public void SetTileData(IntPoint3 p, TileData data)
		{
			this.Grid[p.Z, p.Y, p.X] = data;
		}

		public void SetWaterLevel(IntPoint3 p, byte waterLevel)
		{
			this.Grid[p.Z, p.Y, p.X].WaterLevel = waterLevel;
		}

		public void SetFlags(IntPoint3 p, TileFlags flags)
		{
			this.Grid[p.Z, p.Y, p.X].Flags |= flags;
		}

		public void ClearFlags(IntPoint3 p, TileFlags flags)
		{
			this.Grid[p.Z, p.Y, p.X].Flags &= ~flags;
		}
	}
}
