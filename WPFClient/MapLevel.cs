using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Controls;
using System.Diagnostics;

namespace MyGame
{
	delegate void MapChanged(Location l);

	struct TileData
	{
		public int m_terrainID;
		public List<ClientGameObject> m_contentList;
	}

	class MapLevel
	{
		public event MapChanged MapChanged;

		GrowingLocationGrid<TileData> m_tileGrid;

		public MapLevel()
		{
			m_tileGrid = new GrowingLocationGrid<TileData>(10);

			ItemObject item = new ItemObject(new ObjectID(1234));
			item.Name = "Foo";
			item.SymbolID = 5;
			this.AddObject(item, new Location(3, 3));
		}

		public int Width
		{
			get { return m_tileGrid.Width; }
		}

		public int Height
		{
			get { return m_tileGrid.Height; }
		}

		public IntRect Bounds
		{
			get { return m_tileGrid.Bounds; }
		}

		public int GetTerrainType(Location l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false, false);

			if (block == null)
				return 0;

			return block[l.X, l.Y].m_terrainID;
		}

		public void SetTerrainType(Location l, int terrainID)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, true, true);

			block[l.X, l.Y].m_terrainID = terrainID;

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(MapLocationTerrain[] locInfos)
		{
			//m_terrainData = new LocationGrid<TerrainData>(m_width, m_height);	// xxx clears the old one
			foreach (MapLocationTerrain locInfo in locInfos)
			{
				Location l = locInfo.Location;

				Location bl = l;
				TileData[,] block = m_tileGrid.GetBlock(ref bl, true, true);
				block[bl.X, bl.Y].m_terrainID = locInfo.Terrain;

				if (locInfo.Objects != null)
				{
					foreach(ObjectID oid in locInfo.Objects)
					{
						ClientGameObject ob = ClientGameObject.FindObject(oid);
						if (ob == null)
						{
							ob = new ClientGameObject(oid);
							ob.SymbolID = 4;
							MyDebug.WriteLine("New object {0}", ob);
						}

						MyDebug.WriteLine("{0} AT {1}", ob, l);

						if (ob.Environment == null)
							ob.SetEnvironment(this, l);
						else
							ob.Location = l;

 					}
				}
				if (MapChanged != null)
					MapChanged(l);
			}
		}

		public IList<ClientGameObject> GetContents(Location l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false, false);

			if (block == null)
				return null;

			if (block[l.X, l.Y].m_contentList == null)
				return null;

			return block[l.X, l.Y].m_contentList.AsReadOnly();
		}

		public void RemoveObject(ClientGameObject ob, Location l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false, false);

			Debug.Assert(block != null);
			Debug.Assert(block[l.X, l.Y].m_contentList != null);

			bool removed = block[l.X, l.Y].m_contentList.Remove(ob);

			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void AddObject(ClientGameObject ob, Location l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, true, true);

			Debug.Assert(block != null);

			if (block[l.X, l.Y].m_contentList == null)
				block[l.X, l.Y].m_contentList = new List<ClientGameObject>();

			Debug.Assert(!block[l.X, l.Y].m_contentList.Contains(ob));
			block[l.X, l.Y].m_contentList.Add(ob);

			if (MapChanged != null)
				MapChanged(l);
		}
	}
}
