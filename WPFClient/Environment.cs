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

	class MyGrowingGrid3D : GrowingGrid3DBase<MyGrowingGrid>
	{
		public MyGrowingGrid3D(int blockSize)
			: base(blockSize)
		{
		}

		protected override MyGrowingGrid CreateLevel(int blockSize)
		{
			return new MyGrowingGrid(blockSize);
		}

		public int GetTerrainID(IntPoint3D p)
		{
			var level = base.GetLevel(p.Z, false);
			if (level == null)
				return 0;
			return level.GetTerrainID(new IntPoint(p.X, p.Y));
		}

		public void SetTerrainID(IntPoint3D p, int terrainID)
		{
			var level = base.GetLevel(p.Z, true);
			level.SetTerrainID(new IntPoint(p.X, p.Y), terrainID);
		}

		public List<ClientGameObject> GetContents(IntPoint3D p)
		{
			var level = base.GetLevel(p.Z, false);
			if (level == null)
				return null;
			return level.GetContents(new IntPoint(p.X, p.Y));
		}

		public void AddObject(IntPoint3D p, ClientGameObject ob, bool tail)
		{
			var level = base.GetLevel(p.Z, true);
			level.AddObject(new IntPoint(p.X, p.Y), ob, tail);
		}

		public void RemoveObject(IntPoint3D p, ClientGameObject ob)
		{
			var level = base.GetLevel(p.Z, true);
			level.RemoveObject(new IntPoint(p.X, p.Y), ob);
		}
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
		public event Action<IntPoint3D> MapChanged;

		MyGrowingGrid3D m_tileGrid;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; set; }

		public Environment(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.Version = 1;
			m_tileGrid = new MyGrowingGrid3D(10);
		}

		public Environment(World world, ObjectID objectID, IntCube bounds)
			: base(world, objectID)
		{
			this.Version = 1;
			m_tileGrid = new MyGrowingGrid3D(Math.Max(bounds.Width, bounds.Height));
		}

		public MyGrowingGrid GetLevel(int z)
		{
			return m_tileGrid.GetLevel(z, false);
		}

		public int GetTerrainID(IntPoint3D l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public void SetTerrainID(IntPoint3D l, int terrainID)
		{
			this.Version += 1;

			m_tileGrid.SetTerrainID(l, terrainID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(IEnumerable<ClientMsgs.MapTileData> locInfos)
		{
			this.Version += 1;

			foreach (MapTileData locInfo in locInfos)
			{
				IntPoint3D l = locInfo.Location;

				m_tileGrid.SetTerrainID(l, locInfo.TerrainID);

				if (MapChanged != null)
					MapChanged(l);
			}
		}

		public void SetTerrains(IntCube bounds, IEnumerable<int> terrainIDs)
		{
			var iter = terrainIDs.GetEnumerator();
			foreach (IntPoint3D p in bounds.Range())
			{
				iter.MoveNext();
				int terrainID = iter.Current;
				m_tileGrid.SetTerrainID(p, terrainID);
			}
		}

		public IList<ClientGameObject> GetContents(IntPoint3D l)
		{
			var list = m_tileGrid.GetContents(l);

			if (list == null)
				return null;
			
			return list.AsReadOnly();
		}

		protected override void ChildAdded(ClientGameObject child)
		{
			IntPoint3D l = child.Location;
			m_tileGrid.AddObject(l, child, !child.IsLiving);

			if (MapChanged != null)
				MapChanged(l);
		}

		protected override void ChildRemoved(ClientGameObject child)
		{
			IntPoint3D l = child.Location;
			m_tileGrid.RemoveObject(l, child);

			if (MapChanged != null)
				MapChanged(l);
		}
	}
}
