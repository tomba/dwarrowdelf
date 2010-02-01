using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using MyGame.ClientMsgs;
using System.Collections.ObjectModel;

namespace MyGame.Client
{

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

		public TileData GetTileData(IntPoint3D p)
		{
			var level = base.GetLevel(p.Z, false);
			if (level == null)
				return new TileData();
			return level.GetTileData(new IntPoint(p.X, p.Y));
		}

		public void SetTileData(IntPoint3D p, TileData data)
		{
			var level = base.GetLevel(p.Z, true);
			level.SetTileData(new IntPoint(p.X, p.Y), data);
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
	}

	class MyGrowingGrid : GrowingGrid2DBase<TileData>
	{
		public MyGrowingGrid(int blockSize) : base(blockSize)
		{
		}

		public TileData GetTileData(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return new TileData();
			return block.Grid[block.GetIndex(p)];
		}

		public void SetTileData(IntPoint p, TileData data)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)] = data;
		}

		public InteriorID GetInteriorID(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return 0;
			return block.Grid[block.GetIndex(p)].InteriorID;
		}

		public void SetInteriorID(IntPoint p, InteriorID interiorID)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)].InteriorID = interiorID;
		}

		public FloorID GetFloorID(IntPoint p)
		{
			var block = base.GetBlock(ref p, false);
			if (block == null)
				return 0;
			return block.Grid[block.GetIndex(p)].FloorID;
		}

		public void SetFloorID(IntPoint p, FloorID floorID)
		{
			var block = base.GetBlock(ref p, true);

			block.Grid[block.GetIndex(p)].FloorID = floorID;
		}
	}

	class Environment : ClientGameObject
	{
		public event Action<IntPoint3D> MapChanged;

		MyGrowingGrid3D m_tileGrid;
		Dictionary<IntPoint3D, List<ClientGameObject>> m_objectMap;
		List<ClientGameObject> m_objectList;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; set; }

		public Environment(World world, ObjectID objectID)
			: this(world, objectID, 16)
		{
		}

		public Environment(World world, ObjectID objectID, IntCuboid bounds)
			: this(world, objectID, Math.Max(bounds.Width, bounds.Height))
		{
		}

		public Environment(World world, ObjectID objectID, int blockSize)
			: base(world, objectID)
		{
			this.Version = 1;
			m_tileGrid = new MyGrowingGrid3D(blockSize);
			m_objectMap = new Dictionary<IntPoint3D, List<ClientGameObject>>();
			m_objectList = new List<ClientGameObject>();
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return GetInterior(l).Blocker == false;
		}

		public MyGrowingGrid GetLevel(int z)
		{
			return m_tileGrid.GetLevel(z, false);
		}

		public InteriorInfo GetInterior(IntPoint3D l)
		{
			var id = m_tileGrid.GetInteriorID(l);
			return this.World.AreaData.Terrains.GetInteriorInfo(id);
		}

		public void SetInteriorID(IntPoint3D l, InteriorID interiorID)
		{
			this.Version += 1;

			m_tileGrid.SetInteriorID(l, interiorID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public FloorInfo GetFloor(IntPoint3D l)
		{
			var id = m_tileGrid.GetFloorID(l);
			return this.World.AreaData.Terrains.GetFloorInfo(id);
		}

		public void SetFloorID(IntPoint3D l, FloorID floorID)
		{
			this.Version += 1;

			m_tileGrid.SetFloorID(l, floorID);

			if (MapChanged != null)
				MapChanged(l);
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3D l)
		{
			var id = m_tileGrid.GetTileData(l).InteriorMaterialID;
			return this.World.AreaData.Materials.GetMaterialInfo(id);
		}

		public MaterialInfo GetFloorMaterial(IntPoint3D l)
		{
			var id = m_tileGrid.GetTileData(l).FloorMaterialID;
			return this.World.AreaData.Materials.GetMaterialInfo(id);
		}

		public TileData GetTileData(IntPoint3D p)
		{
			return m_tileGrid.GetTileData(p);
		}

		public void SetTerrains(IEnumerable<KeyValuePair<IntPoint3D, TileData>> tileDataList)
		{
			this.Version += 1;

			foreach (var kvp in tileDataList)
			{
				IntPoint3D p = kvp.Key;
				TileData data = kvp.Value;

				m_tileGrid.SetTileData(p, data);

				if (MapChanged != null)
					MapChanged(p);
			}
		}

		public void SetTerrains(IntCuboid bounds, IEnumerable<TileData> tileDataList)
		{
			this.Version += 1;

			var iter = tileDataList.GetEnumerator();
			foreach (IntPoint3D p in bounds.Range())
			{
				iter.MoveNext();
				TileData data = iter.Current;
				m_tileGrid.SetTileData(p, data);
			}

			if (MapChanged != null)
				MapChanged(new IntPoint3D(bounds.X, bounds.Y, bounds.Z)); // XXX
		}

		ObservableCollection<BuildingData> m_buildings = new ObservableCollection<BuildingData>();

		public void AddBuilding(BuildingData building)
		{
			Debug.Assert(m_buildings.Any(b => b.Z == building.Z && b.Area.IntersectsWith(building.Area)) == false);

			m_buildings.Add(building);
		}

		public void SetBuildings(IEnumerable<ClientMsgs.BuildingData> buildings)
		{
			this.Version += 1;

			var list = buildings.Select(bd => new BuildingData(this.World, bd.ObjectID, bd.ID)
			{
				Area = bd.Area,
				Z = bd.Z,
				Environment = this,
			});

			foreach (var b in list)
				AddBuilding(b);
		}

		public BuildingData GetBuildingAt(IntPoint3D p)
		{
			return m_buildings.SingleOrDefault(b => b.Z == p.Z && b.Area.Contains(p.TwoD));
		}

		public IList<ClientGameObject> GetContents(IntPoint3D l)
		{
			List<ClientGameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return null;

			return obs.AsReadOnly();
		}

		public IList<ClientGameObject> GetContents()
		{
			return m_objectList.AsReadOnly();
		}

		protected override void ChildAdded(ClientGameObject child)
		{
			IntPoint3D l = child.Location;

			List<ClientGameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs))
			{
				obs = new List<ClientGameObject>();
				m_objectMap[l] = obs;
			}

			if (child.IsLiving)
				obs.Insert(0, child);
			else
				obs.Add(child);

			m_objectList.Add(child);

			if (MapChanged != null)
				MapChanged(l);
		}

		protected override void ChildRemoved(ClientGameObject child)
		{
			IntPoint3D l = child.Location;

			Debug.Assert(m_objectMap.ContainsKey(l));

			List<ClientGameObject> obs = m_objectMap[l];

			bool removed = obs.Remove(child);
			Debug.Assert(removed);

			removed = m_objectList.Remove(child);
			Debug.Assert(removed);

			if (MapChanged != null)
				MapChanged(l);
		}
	}
}
