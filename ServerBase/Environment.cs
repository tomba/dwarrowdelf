using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	public delegate void MapChanged(ObjectID mapID, IntPoint l, int terrainID);

	struct TileData
	{
		public int m_terrainID;
		public List<ServerGameObject> m_contentList;
	}

	class TileGrid : Grid2DBase<TileData>
	{
		public TileGrid(int width, int height)
			: base(width, height)
		{
		}

		public void SetTerrainType(IntPoint l, int terrainType)
		{
			base.Grid[GetIndex(l)].m_terrainID = terrainType;
		}

		public int GetTerrainID(IntPoint l)
		{
			return base.Grid[GetIndex(l)].m_terrainID;
		}

		public List<ServerGameObject> GetContentList(IntPoint l)
		{
			return base.Grid[GetIndex(l)].m_contentList;
		}

		public void SetContentList(IntPoint l, List<ServerGameObject> list)
		{
			base.Grid[GetIndex(l)].m_contentList = list;
		}
	}

	public class Environment : ServerGameObject 
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }

		public Environment(World world)
			: base(world)
		{
			this.Version = 1;
			base.Name = "map";
			this.VisibilityMode = VisibilityMode.AllVisible;

			this.Width = 55;
			this.Height = 55;

			m_tileGrid = new TileGrid(this.Width, this.Height);

			Random r = new Random(123);
			TerrainInfo floor = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Floor");
			TerrainInfo wall = world.AreaData.Terrains.Single(t => t.Name == "Dungeon Wall");
			for (int y = 0; y < this.Height; y++)
			{
				for (int x = 0; x < this.Width; x++)
				{
					if (r.Next() % 8 == 0)
						m_tileGrid.SetTerrainType(new IntPoint(x, y), wall.ID);
					else
						m_tileGrid.SetTerrainType(new IntPoint(x, y), floor.ID);
				}
			}

			m_tileGrid.SetTerrainType(new IntPoint(0, 0), floor.ID);
			m_tileGrid.SetTerrainType(new IntPoint(1, 1), floor.ID);
			m_tileGrid.SetTerrainType(new IntPoint(2, 2), floor.ID);
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public int GetTerrainID(IntPoint l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public void SetTerrain(IntPoint l, int terrainID)
		{
			this.Version += 1;

			m_tileGrid.SetTerrainType(l, terrainID);

			if (MapChanged != null)
				MapChanged(this.ObjectID, l, terrainID);
		}

		public bool IsWalkable(IntPoint l)
		{
			return this.World.AreaData.Terrains[GetTerrainID(l)].IsWalkable;
		}

		public IList<ServerGameObject> GetContents(IntPoint l)
		{
			return m_tileGrid.GetContentList(l);
		}

		protected override void ChildAdded(ServerGameObject child)
		{
			IntPoint l = child.Location;

			if (m_tileGrid.GetContentList(l) == null)
				m_tileGrid.SetContentList(l, new List<ServerGameObject>());

			Debug.Assert(!m_tileGrid.GetContentList(l).Contains(child));
			m_tileGrid.GetContentList(l).Add(child);
		}

		protected override void ChildRemoved(ServerGameObject child)
		{
			IntPoint l = child.Location;
			Debug.Assert(m_tileGrid.GetContentList(l) != null);
			bool removed = m_tileGrid.GetContentList(l).Remove(child);
			Debug.Assert(removed);
		}

		protected override bool OkToAddChild(ServerGameObject child, IntPoint p)
		{
			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			return true;
		}

		protected override void ChildMoved(ServerGameObject child, IntPoint oldLocation, IntPoint newLocation)
		{
			Debug.Assert(m_tileGrid.GetContentList(oldLocation) != null);
			bool removed = m_tileGrid.GetContentList(oldLocation).Remove(child);
			Debug.Assert(removed);

			if (m_tileGrid.GetContentList(newLocation) == null)
				m_tileGrid.SetContentList(newLocation, new List<ServerGameObject>());

			Debug.Assert(!m_tileGrid.GetContentList(newLocation).Contains(child));
			m_tileGrid.GetContentList(newLocation).Add(child);
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}
	}
}
