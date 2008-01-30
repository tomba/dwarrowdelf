using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Controls;
using System.Diagnostics;

namespace MyGame
{
	delegate void MapChanged(Location l);

	struct TerrainData
	{
		public int m_terrainID;

		public TerrainData(int terrainID)
		{
			m_terrainID = terrainID;
		}
	}

	class MapLevel
	{
		public event MapChanged MapChanged;

		LocationGrid<TerrainData> m_terrainData;
		LocationGrid<List<ClientGameObject>> m_mapContents;
		int m_width;
		int m_height;

		public MapLevel(int width, int height)
		{
			m_width = width;
			m_height = height;
			m_terrainData = new LocationGrid<TerrainData>(width, height);
			m_mapContents = new LocationGrid<List<ClientGameObject>>(m_width, m_height);
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
		}

		public LocationGrid<TerrainData> GetTerrain()
		{
			return m_terrainData;
		}

		public int GetTerrain(Location l)
		{
			return m_terrainData[l].m_terrainID;
		}

		public void SetTerrain(Location topLeft, int width, int height, int[] data)
		{
			int i = 0;
			for (int y = topLeft.Y; y < topLeft.Y + height; y++)
				for (int x = topLeft.X; x < topLeft.X + width; x++)
				{
					m_terrainData[x, y] = new TerrainData(data[i++]);

					if (MapChanged != null)
						MapChanged(new Location(x, y));
				}
		}

		public void SetTerrains(MapLocation[] locations)
		{
			//m_terrainData = new int[m_width, m_height]; // xxx clears the old one
			foreach (MapLocation l in locations)
			{
				m_terrainData[l.Location] = new TerrainData(l.Terrain);
				if (MapChanged != null)
					MapChanged(l.Location);
			}
		}

		public List<ClientGameObject> GetContents(Location l)
		{
			return m_mapContents[l];
		}

		public void SetContents(Location l, int[] symbols)
		{
			throw new NotImplementedException();
			/*
			m_mapContents[l] = new List<int>(symbols);
			MapChanged(l);
			 */
		}

		public void RemoveObject(ClientGameObject ob, Location l)
		{
			Debug.Assert(m_mapContents[l] != null);
			bool removed = m_mapContents[l].Remove(ob);
			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void AddObject(ClientGameObject ob, Location l)
		{
			if (m_mapContents[l] == null)
				m_mapContents[l] = new List<ClientGameObject>();
			m_mapContents[l].Add(ob);

			if (MapChanged != null)
				MapChanged(l);
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
