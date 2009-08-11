using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MyGame.ClientMsgs;

namespace MyGame
{
	delegate void MapChanged(IntPoint l);

	struct TileData
	{
		public int m_terrainID;
		public List<ClientGameObject> m_contentList;
	}

	class MapLevel : ClientGameObject
	{
		public event MapChanged MapChanged;

		GrowingLocationGrid<TileData> m_tileGrid;

		public uint Version { get; private set; }

		public MapLevel(ObjectID objectID) : base(objectID)
		{
			this.Version = 1;
			m_tileGrid = new GrowingLocationGrid<TileData>(10);
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

		public int GetTerrainType(IntPoint l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false);

			if (block == null)
				return 0;

			return block[l.X, l.Y].m_terrainID;
		}

		public void SetTerrainType(IntPoint l, int terrainID)
		{
			this.Version += 1;

			TileData[,] block = m_tileGrid.GetBlock(ref l, true);

			block[l.X, l.Y].m_terrainID = terrainID;

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(ClientMsgs.MapTileData[] locInfos)
		{
			this.Version += 1;

			//m_terrainData = new LocationGrid<TerrainData>(m_width, m_height);	// xxx clears the old one
			foreach (MapTileData locInfo in locInfos)
			{
				IntPoint l = locInfo.Location;

				IntPoint bl = l;
				TileData[,] block = m_tileGrid.GetBlock(ref bl, true);
				block[bl.X, bl.Y].m_terrainID = locInfo.Terrain;

				/*
				if (locInfo.Objects != null)
				{
					foreach(ObjectID oid in locInfo.Objects)
					{
						ClientGameObject ob = ClientGameObject.FindObject(oid);
						if (ob == null)
						{
							ob = new ClientGameObject(oid);
							//ob.SymbolID = 4;
							MyDebug.WriteLine("New object {0}", ob);
						}

						MyDebug.WriteLine("{0} AT {1}", ob, l);

						if (ob.Environment == null)
							ob.SetEnvironment(this, l);
						else
							ob.Location = l;

 					}
				}
				 */
				if (MapChanged != null)
					MapChanged(l);
			}
		}

		public IList<ClientGameObject> GetContents(IntPoint l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false);

			if (block == null)
				return null;

			if (block[l.X, l.Y].m_contentList == null)
				return null;

			return block[l.X, l.Y].m_contentList.AsReadOnly();
		}

		public void RemoveObject(ClientGameObject ob, IntPoint l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, false);

			Debug.Assert(block != null);
			Debug.Assert(block[l.X, l.Y].m_contentList != null);

			bool removed = block[l.X, l.Y].m_contentList.Remove(ob);

			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void AddObject(ClientGameObject ob, IntPoint l)
		{
			TileData[,] block = m_tileGrid.GetBlock(ref l, true);

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
