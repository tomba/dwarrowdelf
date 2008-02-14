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

	class MapLevel : GrowingLocationGrid<TileData>
	{
		public event MapChanged MapChanged;

		public MapLevel(int width, int height) : base(8)
		{
		}
/*
		public IntRect Bounds
		{
			get { return new IntRect(0, 0, m_width, m_height); }
		}
*/
		public int GetTerrainType(Location l)
		{
			int x = l.X;
			int y = l.Y;

			TileData[,] block = base.GetBlock(ref x, ref y, false, false);

			if (block == null)
				return 0;

			return block[x, y].m_terrainID;
		}

		public void SetTerrainType(Location l, int terrainID)
		{
			int x = l.X;
			int y = l.Y;

			TileData[,] block = base.GetBlock(ref x, ref y, true, true);

			block[x, y].m_terrainID = terrainID;

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(MapLocationTerrain[] locInfos)
		{
			//m_terrainData = new LocationGrid<TerrainData>(m_width, m_height);	// xxx clears the old one
			foreach (MapLocationTerrain locInfo in locInfos)
			{
				Location l = locInfo.Location;

				int x = l.X;
				int y = l.Y;

				TileData[,] block = base.GetBlock(ref x, ref y, true, true);

				block[x, y].m_terrainID = locInfo.Terrain;

				if (locInfo.Objects != null)
				{
					foreach(ObjectID oid in locInfo.Objects)
					{
						ClientGameObject ob = ClientGameObject.FindObject(oid);
						if (ob == null)
						{
							MyDebug.WriteLine("New object {0}", oid);
							ob = new ClientGameObject(oid);
						}

						MyDebug.WriteLine("OB AT {0}", l);

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

		public List<ClientGameObject> GetContents(Location l)
		{
			int x = l.X;
			int y = l.Y;

			TileData[,] block = base.GetBlock(ref x, ref y, false, false);

			if (block == null)
				return null;

			return block[x, y].m_contentList;
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
			int x = l.X;
			int y = l.Y;

			TileData[,] block = base.GetBlock(ref x, ref y, false, false);

			Debug.Assert(block != null);
			Debug.Assert(block[x, y].m_contentList != null);

			bool removed = block[x, y].m_contentList.Remove(ob);

			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void AddObject(ClientGameObject ob, Location l)
		{
			int x = l.X;
			int y = l.Y;

			TileData[,] block = base.GetBlock(ref x, ref y, true, true);

			Debug.Assert(block != null);

			if (block[x, y].m_contentList == null)
				block[x, y].m_contentList = new List<ClientGameObject>();

			Debug.Assert(!block[x, y].m_contentList.Contains(ob));
			block[x, y].m_contentList.Add(ob);

			if (MapChanged != null)
				MapChanged(l);
		}
		/*
		public int Width
		{
			get { return m_width; }
		}

		public int Height
		{
			get { return m_height; }
		}
		*/

	}
}
