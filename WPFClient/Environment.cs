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
		public InteriorID m_interiorID;
		public FloorID m_floorID;
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

		public InteriorID GetInteriorID(IntPoint3D p)
		{
			var level = base.GetLevel(p.Z, false);
			if (level == null)
				return 0;
			return level.GetInteriorID(new IntPoint(p.X, p.Y));
		}

		public void SetInteriorID(IntPoint3D p, InteriorID interiorID)
		{
			var level = base.GetLevel(p.Z, true);
			level.SetInteriorID(new IntPoint(p.X, p.Y), interiorID);
		}

		public FloorID GetFloorID(IntPoint3D p)
		{
			var level = base.GetLevel(p.Z, false);
			if (level == null)
				return 0;
			return level.GetFloorID(new IntPoint(p.X, p.Y));
		}

		public void SetFloorID(IntPoint3D p, FloorID floorID)
		{
			var level = base.GetLevel(p.Z, true);
			level.SetFloorID(new IntPoint(p.X, p.Y), floorID);
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

		public InteriorID GetInteriorID(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return 0;
			return block.Grid[block.GetIndex(p)].m_interiorID;
		}

		public void SetInteriorID(IntPoint p, InteriorID interiorID)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)].m_interiorID = interiorID;
		}

		public FloorID GetFloorID(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return 0;
			return block.Grid[block.GetIndex(p)].m_floorID;
		}

		public void SetFloorID(IntPoint p, FloorID floorID)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)].m_floorID = floorID;
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

		public bool IsWalkable(IntPoint3D l)
		{
			return !this.World.AreaData.Terrains.GetInteriorInfo(GetInteriorID(l)).Blocker;
		}

		public MyGrowingGrid GetLevel(int z)
		{
			return m_tileGrid.GetLevel(z, false);
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public void SetInteriorID(IntPoint3D l, InteriorID interiorID)
		{
			this.Version += 1;

			m_tileGrid.SetInteriorID(l, interiorID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public FloorID GetFloorID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorID(l);
		}

		public void SetFloorID(IntPoint3D l, FloorID floorID)
		{
			this.Version += 1;

			m_tileGrid.SetFloorID(l, floorID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public void SetTerrains(IEnumerable<ClientMsgs.MapTileData> locInfos)
		{
			this.Version += 1;

			foreach (MapTileData locInfo in locInfos)
			{
				IntPoint3D l = locInfo.Location;

				m_tileGrid.SetInteriorID(l, locInfo.TileData.m_interiorID);
				m_tileGrid.SetFloorID(l, locInfo.TileData.m_floorID);

				if (MapChanged != null)
					MapChanged(l);
			}
		}

		public void SetTerrains(IntCube bounds, IEnumerable<TileIDs> tileIDList)
		{
			var iter = tileIDList.GetEnumerator();
			foreach (IntPoint3D p in bounds.Range())
			{
				iter.MoveNext();
				var td = iter.Current;
				m_tileGrid.SetInteriorID(p, td.m_interiorID);
				m_tileGrid.SetFloorID(p, td.m_floorID);
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
