using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MyGame.ClientMsgs;

namespace MyGame
{
	struct TileData
	{
		public int m_terrainID;
		public List<ClientGameObject> m_contentList;
	}

	class MyGrowingGrid : GrowingGrid2DBase<TileData>
	{
		public MyGrowingGrid(int blockSize) : base(blockSize)
		{
		}

		public int GetTerrainID(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return 0;
			return block.Grid[block.GetIndex(p)].m_terrainID;
		}

		public void SetTerrainID(IntPoint p, int terrainID)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)].m_terrainID = terrainID;
		}

		public List<ClientGameObject> GetContents(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);

			if (block == null)
				return null;

			return block.Grid[block.GetIndex(p)].m_contentList;
		}

		public void AddObject(IntPoint p, ClientGameObject ob, bool tail)
		{
			var block = base.GetBlock(ref p, true);

			Debug.Assert(block != null);

			int idx = block.GetIndex(p);

			var list = block.Grid[idx].m_contentList;

			if (list == null)
			{
				list = new List<ClientGameObject>();
				block.Grid[idx].m_contentList = list;
			}

			Debug.Assert(!list.Contains(ob));
			if (tail)
				list.Add(ob);
			else
				list.Insert(0, ob);
		}

		public void RemoveObject(IntPoint p, ClientGameObject ob)
		{
			var block = base.GetBlock(ref p, false);

			bool removed = block.Grid[block.GetIndex(p)].m_contentList.Remove(ob);

			Debug.Assert(removed);
		}
	}

	class Environment : ClientGameObject
	{
		public event Action<IntPoint> MapChanged;

		MyGrowingGrid m_tileGrid;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; set; }

		public World World { get; private set; }

		public Environment(World world, ObjectID objectID) : base(objectID)
		{
			this.Version = 1;
			this.World = world;
			m_tileGrid = new MyGrowingGrid(10);
		}

		public int Width { get { return m_tileGrid.Width; } }

		public int Height { get { return m_tileGrid.Height; } }

		public IntRect Bounds { get { return m_tileGrid.Bounds; } }

		public int GetTerrainID(IntPoint l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public void SetTerrainID(IntPoint l, int terrainID)
		{
			this.Version += 1;

			m_tileGrid.SetTerrainID(l, terrainID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(ClientMsgs.MapTileData[] locInfos)
		{
			this.Version += 1;

			foreach (MapTileData locInfo in locInfos)
			{
				IntPoint l = locInfo.Location;

				m_tileGrid.SetTerrainID(l, locInfo.TerrainID);

				if (MapChanged != null)
					MapChanged(l);
			}
		}

		public IList<ClientGameObject> GetContents(IntPoint l)
		{
			var list = m_tileGrid.GetContents(l);

			if (list == null)
				return null;
			
			return list.AsReadOnly();
		}

		protected override void ChildAdded(ClientGameObject child)
		{
			IntPoint l = child.Location;
			m_tileGrid.AddObject(l, child, !child.IsLiving);

			if (MapChanged != null)
				MapChanged(l);
		}

		protected override void ChildRemoved(ClientGameObject child)
		{
			IntPoint l = child.Location;
			m_tileGrid.RemoveObject(l, child);

			if (MapChanged != null)
				MapChanged(l);
		}
	}
}
