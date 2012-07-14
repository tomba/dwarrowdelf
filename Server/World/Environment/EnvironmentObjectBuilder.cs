using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed class EnvironmentObjectBuilder
	{
		IntSize3 m_size;
		TileGrid m_tileGrid;
		ArrayGrid2D<byte> m_depthMap;

		internal TileGrid Grid { get { return m_tileGrid; } }
		internal ArrayGrid2D<byte> DepthMap { get { return m_depthMap; } }

		public IntGrid3 Bounds { get { return new IntGrid3(m_size); } }
		public int Width { get { return m_size.Width; } }
		public int Height { get { return m_size.Height; } }
		public int Depth { get { return m_size.Depth; } }

		public VisibilityMode VisibilityMode { get; set; }

		public EnvironmentObjectBuilder(TileGrid grid, ArrayGrid2D<byte> depthMap, VisibilityMode visibilityMode)
		{
			m_depthMap = depthMap;
			m_tileGrid = grid;
			m_size = grid.Size;

			this.VisibilityMode = visibilityMode;
		}

		public EnvironmentObject Create(World world)
		{
			return EnvironmentObject.Create(world, this);
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		public int GetDepth(IntPoint2 p)
		{
			return m_depthMap[p];
		}

		public TerrainID GetTerrainID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public TerrainInfo GetTerrain(IntPoint3 l)
		{
			return Terrains.GetTerrain(GetTerrainID(l));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetTerrainMaterialID(l));
		}

		public InteriorInfo GetInterior(IntPoint3 l)
		{
			return Interiors.GetInterior(GetInteriorID(l));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetInteriorMaterialID(l));
		}

		public TileData GetTileData(IntPoint3 l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetTileFlag(IntPoint3 l, TileFlags flag)
		{
			return (m_tileGrid.GetFlags(l) & flag) != 0;
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			var p2d = p.ToIntPoint();

			if (data.IsEmpty == false && m_depthMap[p2d] < p.Z)
			{
				Debug.Assert(p.Z >= 0 && p.Z < 256);
				m_depthMap[p2d] = (byte)p.Z;
			}
			else if (data.IsEmpty && m_depthMap[p2d] == p.Z)
			{
				if (p.Z == 0)
					throw new Exception();

				for (int z = p.Z - 1; z >= 0; --z)
				{
					if (m_tileGrid.GetTileData(new IntPoint3(p2d, z)).IsEmpty == false)
					{
						Debug.Assert(z >= 0 && z < 256);
						m_depthMap[p2d] = (byte)z;
						break;
					}
				}
			}

			m_tileGrid.SetTileData(p, data);
		}

		public void SetTileFlags(IntPoint3 l, TileFlags flags, bool value)
		{
			if (value)
				m_tileGrid.SetFlags(l, flags);
			else
				m_tileGrid.ClearFlags(l, flags);
		}
	}
}
