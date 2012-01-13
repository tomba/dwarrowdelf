using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	public sealed class GrowingTileGrid
	{
		TileData[, ,] m_grid;
		IntCuboid m_bounds;

		public GrowingTileGrid()
			: this(new IntCuboid())
		{
		}

		public GrowingTileGrid(IntCuboid bounds)
		{
			SetBounds(bounds);
		}

		public void SetBounds(IntCuboid bounds)
		{
			if (!m_bounds.IsNull)
				throw new Exception();

			int nx = bounds.X;
			int ny = bounds.Y;
			int nz = bounds.Z;
			int nw = bounds.Width;
			int nh = bounds.Height;
			int nd = bounds.Depth;

			m_grid = new TileData[nd, nh, nw];
			m_bounds = new IntCuboid(nx, ny, nz, nw, nh, nd);

			Debug.Print("GrowingTileGrid.SetBounds({0})", m_bounds);
		}

		int Align256(int x)
		{
			if (x >= 0)
				return (x + 0xff) & ~0xff;
			else
				return x & ~0xff;
		}

		int Align16(int x)
		{
			if (x >= 0)
				return (x + 0xf) & ~0xf;
			else
				return x & ~0xf;
		}

		bool Adjust(ref IntPoint3 p)
		{
			if (!m_bounds.Contains(p))
				return false;

			p += new IntVector3(-m_bounds.X1, -m_bounds.Y1, -m_bounds.Z1);
			return true;
		}

		void Grow(ref IntPoint3 p)
		{
			if (!m_bounds.Contains(p))
				DoGrow(p);

			p += new IntVector3(-m_bounds.X1, -m_bounds.Y1, -m_bounds.Z1);
		}

		void DoGrow(IntPoint3 p)
		{
			int nx, ny, nz;
			int nw, nh, nd;

			if (p.X >= m_bounds.X2)
			{
				nw = Align256(m_bounds.Width + (p.X - (m_bounds.X2 - 1)));
				nx = m_bounds.X1;
			}
			else if (p.X < m_bounds.X1)
			{
				nw = Align256(m_bounds.Width + (m_bounds.X1 - p.X));
				nx = m_bounds.X1 - (nw - m_bounds.Width);
			}
			else
			{
				nw = m_bounds.Width;
				nx = m_bounds.X1;
			}

			if (p.Y >= m_bounds.Y2)
			{
				nh = Align256(m_bounds.Height + (p.Y - (m_bounds.Y2 - 1)));
				ny = m_bounds.Y1;
			}
			else if (p.Y < m_bounds.Y1)
			{
				nh = Align256(m_bounds.Height + (m_bounds.Y1 - p.Y));
				ny = m_bounds.Y1 - (nh - m_bounds.Height);
			}
			else
			{
				nh = m_bounds.Height;
				ny = m_bounds.Y1;
			}

			if (p.Z >= m_bounds.Z2)
			{
				nd = Align16(m_bounds.Depth + (p.Z - (m_bounds.Z2 - 1)));
				nz = m_bounds.Z1;
			}
			else if (p.Z < m_bounds.Z1)
			{
				nd = Align16(m_bounds.Depth + (m_bounds.Z1 - p.Z));
				nz = m_bounds.Z1 - (nd - m_bounds.Depth);
			}
			else
			{
				nd = m_bounds.Depth;
				nz = m_bounds.Z1;
			}

			var oldOrigin = new IntVector3(-m_bounds.X1, -m_bounds.Y1, -m_bounds.Z1);
			var newOrigin = new IntVector3(-nx, -ny, -nz);
			var newGrid = new TileData[nd, nh, nw];

			/* XXX Array.Copy will probably give better speed */
			foreach (var l in m_bounds.Range())
			{
				var sp = l + oldOrigin;
				var dp = l + newOrigin;
				var src = m_grid[sp.Z, sp.Y, sp.X];
				newGrid[dp.Z, dp.Y, dp.X] = src;
			}

			m_grid = newGrid;
			m_bounds = new IntCuboid(nx, ny, nz, nw, nh, nd);

			Debug.Print("GrowingTileGrid.SetBounds({0})", m_bounds);
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X] = data;
		}

		public TileData GetTileData(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return new TileData();
			return m_grid[p.Z, p.Y, p.X];
		}

		public void SetInteriorID(IntPoint3 p, InteriorID id)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].InteriorID = id;
		}

		public InteriorID GetInteriorID(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return InteriorID.Undefined;
			return m_grid[p.Z, p.Y, p.X].InteriorID;
		}

		public void SetTerrainID(IntPoint3 p, TerrainID id)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].TerrainID = id;
		}

		public TerrainID GetTerrainID(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return TerrainID.Undefined;
			return m_grid[p.Z, p.Y, p.X].TerrainID;
		}


		public void SetInteriorMaterialID(IntPoint3 p, MaterialID id)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].InteriorMaterialID = id;
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return MaterialID.Undefined;
			return m_grid[p.Z, p.Y, p.X].InteriorMaterialID;
		}

		public void SetTerrainMaterialID(IntPoint3 p, MaterialID id)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].TerrainMaterialID = id;
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return MaterialID.Undefined;
			return m_grid[p.Z, p.Y, p.X].TerrainMaterialID;
		}

		public void SetWaterLevel(IntPoint3 p, byte waterLevel)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].WaterLevel = waterLevel;
		}

		public byte GetWaterLevel(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return 0;
			return m_grid[p.Z, p.Y, p.X].WaterLevel;
		}

		public void SetFlags(IntPoint3 p, TileFlags flags)
		{
			Grow(ref p);
			m_grid[p.Z, p.Y, p.X].Flags = flags;
		}

		public TileFlags GetFlags(IntPoint3 p)
		{
			if (!Adjust(ref p))
				return TileFlags.None;
			return m_grid[p.Z, p.Y, p.X].Flags;
		}
	}
}
