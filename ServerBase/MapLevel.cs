using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void MapChanged(ObjectID mapID, IntPoint l, int terrainID);

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

		public int GetTerrainType(IntPoint l)
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

	class MapLevel : ServerGameObject 
	{
		public WorldDefinition Area { get; protected set; }
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		List<ServerGameObject> m_containedObjects; // objects on this level
		int m_width;
		int m_height;


		public MapLevel(WorldDefinition area) : base(area.World)
		{
			this.Area = area;
			base.Name = "map";

			m_width = 55;
			m_height = 55;

			m_tileGrid = new TileGrid(m_width, m_height);

			Random r = new Random();
			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					if (r.Next() % 8 == 0)
						m_tileGrid.SetTerrainType(new IntPoint(x, y), 2); // wall
					else
						m_tileGrid.SetTerrainType(new IntPoint(x, y), 1); // fill with floor tiles
				}
			}

			m_tileGrid.SetTerrainType(new IntPoint(0, 0), 1);
			m_tileGrid.SetTerrainType(new IntPoint(1, 1), 1);
			m_tileGrid.SetTerrainType(new IntPoint(2, 2), 1);

			m_containedObjects = new List<ServerGameObject>();
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
		}

		public int GetTerrain(IntPoint l)
		{
			return m_tileGrid.GetTerrainType(l);
		}

		public void SetTerrain(IntPoint l, int terrainID)
		{
			m_tileGrid.SetTerrainType(l, terrainID);

			if (MapChanged != null)
				MapChanged(this.ObjectID, l, terrainID);
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
