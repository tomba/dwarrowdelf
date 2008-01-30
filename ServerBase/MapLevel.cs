using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void MapChanged(Location l);

	class MapLevel : IIdentifiable
	{
		public AreaDefinition Area { get; protected set; }
		public event MapChanged MapChanged;

		LocationGrid<int> m_mapTerrains;
		LocationGrid<List<ServerGameObject>> m_mapContents;
		List<ServerGameObject> m_containedObjects; // objects on this level
		int m_width;
		int m_height;


		readonly ObjectID m_mapID = new ObjectID(123);

		public MapLevel(AreaDefinition area)
		{
			this.Area = area;

			m_width = 80;
			m_height = 20;

			m_mapTerrains = new LocationGrid<int>(m_width, m_height);

			for (int y = 0; y < m_height; y++)
			{
				for (int x = 0; x < m_width; x++)
				{
					m_mapTerrains[x, y] = 1; // fill with floor tiles
				}
			}

			m_mapTerrains[4, 1] = 2;
			m_mapTerrains[5, 1] = 2;
			m_mapTerrains[5, 2] = 2;
			m_mapTerrains[5, 3] = 2;

			m_mapContents = new LocationGrid<List<ServerGameObject>>(m_width, m_height);

			m_containedObjects = new List<ServerGameObject>();
		}

		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
		}

		public LocationGrid<int> GetTerrain()
		{
			return m_mapTerrains;
		}

		public int GetTerrain(Location l)
		{
			return m_mapTerrains[l];
		}

		public void SetTerrain(Location l, int terrainID)
		{
			m_mapTerrains[l] = terrainID;

			if (MapChanged != null)
				MapChanged(l);
		}

		public List<ServerGameObject> GetContents(Location l)
		{
			return m_mapContents[l];
		}

		public void RemoveObject(ServerGameObject ob, Location l)
		{
			Debug.Assert(m_mapContents[l] != null);
			bool removed = m_mapContents[l].Remove(ob);
			Debug.Assert(removed);

			removed = m_containedObjects.Remove(ob);
			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void AddObject(ServerGameObject ob, Location l)
		{
			if (m_mapContents[l] == null)
				m_mapContents[l] = new List<ServerGameObject>();

			Debug.Assert(!m_mapContents[l].Contains(ob));
			m_mapContents[l].Add(ob);

			Debug.Assert(!m_containedObjects.Contains(ob));
			m_containedObjects.Add(ob);

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

		#region IIdentifiable Members

		public ObjectID ObjectID
		{
			get { return m_mapID; }
		}

		#endregion
	}
}
