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

	class TileGrid
	{
		TileData[,] m_tileGrid;

		public TileGrid(int width, int height)
		{
			m_tileGrid = new TileData[width, height];
		}

		public void SetTerrainType(IntPoint l, int terrainType)
		{
			m_tileGrid[l.X, l.Y].m_terrainID = terrainType;
		}

		public int GetTerrainID(IntPoint l)
		{
			return m_tileGrid[l.X, l.Y].m_terrainID;
		}

		public List<ServerGameObject> GetContentList(IntPoint l)
		{
			return m_tileGrid[l.X, l.Y].m_contentList;
		}

		public void SetContentList(IntPoint l, List<ServerGameObject> list)
		{
			m_tileGrid[l.X, l.Y].m_contentList = list;
		}

	}

	public class Environment : ServerGameObject 
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		List<ServerGameObject> m_containedObjects; // objects on this level
		int m_width;
		int m_height;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }

		public Environment(World world)
			: base(world)
		{
			this.Version = 1;
			base.Name = "map";
			this.VisibilityMode = VisibilityMode.SimpleFOV;

			m_width = 55;
			m_height = 55;

			m_tileGrid = new TileGrid(m_width, m_height);

			Random r = new Random(123);
			TerrainInfo floor = world.Terrains.FindTerrainByName("Dungeon Floor");
			TerrainInfo wall = world.Terrains.FindTerrainByName("Dungeon Wall");
			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					if (r.Next() % 8 == 0)
						m_tileGrid.SetTerrainType(new IntPoint(x, y), wall.TerrainID);
					else
						m_tileGrid.SetTerrainType(new IntPoint(x, y), floor.TerrainID);
				}
			}

			m_tileGrid.SetTerrainType(new IntPoint(0, 0), floor.TerrainID);
			m_tileGrid.SetTerrainType(new IntPoint(1, 1), floor.TerrainID);
			m_tileGrid.SetTerrainType(new IntPoint(2, 2), floor.TerrainID);

			m_containedObjects = new List<ServerGameObject>();
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
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
			return this.World.Terrains[GetTerrainID(l)].IsWalkable;
		}

		public List<ServerGameObject> GetContents(IntPoint l)
		{
			return m_tileGrid.GetContentList(l);
		}

		public void RemoveObject(ServerGameObject ob, IntPoint l)
		{
			Debug.Assert(m_tileGrid.GetContentList(l) != null);
			bool removed = m_tileGrid.GetContentList(l).Remove(ob);
			Debug.Assert(removed);

			removed = m_containedObjects.Remove(ob);
			Debug.Assert(removed);
		}

		public void AddObject(ServerGameObject ob, IntPoint l)
		{
			if (m_tileGrid.GetContentList(l) == null)
				m_tileGrid.SetContentList(l, new List<ServerGameObject>());

			Debug.Assert(!m_tileGrid.GetContentList(l).Contains(ob));
			m_tileGrid.GetContentList(l).Add(ob);

			Debug.Assert(!m_containedObjects.Contains(ob));
			m_containedObjects.Add(ob);
		}

		public int Width
		{
			get { return m_width; }
		}

		public int Height
		{
			get { return m_height; }
		}
	}
}
