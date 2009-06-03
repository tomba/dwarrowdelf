using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void MapChanged(ObjectID mapID, Location l, int terrainID);

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

		public void SetTerrainType(Location l, int terrainType)
		{
			m_tileGrid[l.X, l.Y].m_terrainID = terrainType;
		}

		public int GetTerrainType(Location l)
		{
			return m_tileGrid[l.X, l.Y].m_terrainID;
		}

		public List<ServerGameObject> GetContentList(Location l)
		{
			return m_tileGrid[l.X, l.Y].m_contentList;
		}

		public void SetContentList(Location l, List<ServerGameObject> list)
		{
			m_tileGrid[l.X, l.Y].m_contentList = list;
		}

	}

	class MapLevel : IIdentifiable
	{
		public WorldDefinition Area { get; protected set; }
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		List<ServerGameObject> m_containedObjects; // objects on this level
		int m_width;
		int m_height;


		readonly ObjectID m_mapID = new ObjectID(123);

		public MapLevel(WorldDefinition area)
		{
			this.Area = area;

			m_width = 80;
			m_height = 20;

			m_tileGrid = new TileGrid(m_width, m_height);

			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					m_tileGrid.SetTerrainType(new Location(x, y), 1); // fill with floor tiles
				}
			}

			m_tileGrid.SetTerrainType(new Location(4, 1), 2);
			m_tileGrid.SetTerrainType(new Location(5, 1), 2);
			m_tileGrid.SetTerrainType(new Location(5, 2), 2);
			m_tileGrid.SetTerrainType(new Location(5, 3), 2);

			m_containedObjects = new List<ServerGameObject>();
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
		}

		public int GetTerrain(Location l)
		{
			return m_tileGrid.GetTerrainType(l);
		}

		public void SetTerrain(Location l, int terrainID)
		{
			m_tileGrid.SetTerrainType(l, terrainID);

			if (MapChanged != null)
				MapChanged(this.ObjectID, l, terrainID);
		}

		public List<ServerGameObject> GetContents(Location l)
		{
			return m_tileGrid.GetContentList(l);
		}

		public void RemoveObject(ServerGameObject ob, Location l)
		{
			Debug.Assert(m_tileGrid.GetContentList(l) != null);
			bool removed = m_tileGrid.GetContentList(l).Remove(ob);
			Debug.Assert(removed);

			removed = m_containedObjects.Remove(ob);
			Debug.Assert(removed);
		}

		public void AddObject(ServerGameObject ob, Location l)
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

		#region IIdentifiable Members

		public ObjectID ObjectID
		{
			get { return m_mapID; }
		}

		#endregion
	}
}
