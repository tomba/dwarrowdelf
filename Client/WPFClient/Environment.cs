using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{

	class Environment : ClientGameObject, IEnvironment
	{
		public event Action<IntPoint3D> MapTileChanged;

		GrowingTileGrid m_tileGrid;
		Dictionary<IntPoint3D, List<ClientGameObject>> m_objectMap;
		List<ClientGameObject> m_objectList;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; set; }

		public IntCuboid Bounds { get; private set; }

		BuildingCollection m_buildings;
		public ReadOnlyBuildingCollection Buildings { get; private set; }

		ObservableCollection<Stockpile> m_stockpiles;
		public ReadOnlyObservableCollection<Stockpile> Stockpiles { get; private set; }

		public IntPoint3D HomeLocation { get; private set; }

		public Environment(World world, ObjectID objectID, IntPoint3D homeLocation)
			: this(world, objectID, new IntCuboid(), homeLocation)
		{
		}

		public Environment(World world, ObjectID objectID, IntCuboid bounds, IntPoint3D homeLocation)
			: base(world, objectID)
		{
			this.Version = 1;

			this.Bounds = bounds;
			this.HomeLocation = homeLocation;

			m_tileGrid = new GrowingTileGrid(bounds);
			m_objectMap = new Dictionary<IntPoint3D, List<ClientGameObject>>();
			m_objectList = new List<ClientGameObject>();

			m_buildings = new BuildingCollection();
			this.Buildings = new ReadOnlyBuildingCollection(m_buildings);

			m_stockpiles = new ObservableCollection<Stockpile>();
			this.Stockpiles = new ReadOnlyObservableCollection<Stockpile>(m_stockpiles);

			this.World.AddEnvironment(this);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return GetInterior(l).Blocker == false;
		}

		public FloorID GetFloorID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorID(l);
		}

		public MaterialID GetFloorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public FloorInfo GetFloor(IntPoint3D l)
		{
			return Floors.GetFloor(GetFloorID(l));
		}

		public MaterialInfo GetFloorMaterial(IntPoint3D l)
		{
			return Materials.GetMaterial(m_tileGrid.GetFloorMaterialID(l));
		}

		public InteriorInfo GetInterior(IntPoint3D l)
		{
			return Interiors.GetInterior(GetInteriorID(l));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3D l)
		{
			return Materials.GetMaterial(m_tileGrid.GetInteriorMaterialID(l));
		}

		public void SetInteriorID(IntPoint3D l, InteriorID interiorID)
		{
			this.Version += 1;

			m_tileGrid.SetInteriorID(l, interiorID);

			if (MapTileChanged != null)
				MapTileChanged(l);
		}

		public void SetFloorID(IntPoint3D l, FloorID floorID)
		{
			this.Version += 1;

			m_tileGrid.SetFloorID(l, floorID);

			if (MapTileChanged != null)
				MapTileChanged(l);
		}

		public byte GetWaterLevel(IntPoint3D l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetGrass(IntPoint3D ml)
		{
			return m_tileGrid.GetGrass(ml);
		}

		public TileData GetTileData(IntPoint3D p)
		{
			return m_tileGrid.GetTileData(p);
		}

		public void SetTileData(IntPoint3D l, TileData tileData)
		{
			this.Version += 1;

			m_tileGrid.SetTileData(l, tileData);

			if (MapTileChanged != null)
				MapTileChanged(l);
		}

		public void SetTerrains(Tuple<IntPoint3D, TileData>[] tileDataList)
		{
			this.Version += 1;

			int x1; int x2;
			int y1; int y2;
			int z1; int z2;

			if (this.Bounds.IsNull)
			{
				x1 = y1 = z1 = Int32.MaxValue;
				x2 = y2 = z2 = Int32.MinValue;
			}
			else
			{
				x1 = this.Bounds.X1;
				x2 = this.Bounds.X2;
				y1 = this.Bounds.Y1;
				y2 = this.Bounds.Y2;
				z1 = this.Bounds.Z1;
				z2 = this.Bounds.Z2;
			}

			bool setNewBounds = false;

			foreach (var kvp in tileDataList)
			{
				setNewBounds = true;
				IntPoint3D p = kvp.Item1;
				TileData data = kvp.Item2;

				x1 = Math.Min(x1, p.X);
				x2 = Math.Max(x2, p.X + 1);
				y1 = Math.Min(y1, p.Y);
				y2 = Math.Max(y2, p.Y + 1);
				z1 = Math.Min(z1, p.Z);
				z2 = Math.Max(z2, p.Z + 1);

				m_tileGrid.SetTileData(p, data);

				if (MapTileChanged != null)
					MapTileChanged(p);
			}

			if (setNewBounds)
			{
				this.Bounds = new IntCuboid(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);
			}
		}

		public void SetTerrains(IntCuboid bounds, IEnumerable<TileData> tileDataList)
		{
			this.Version += 1;

			int x1; int x2;
			int y1; int y2;
			int z1; int z2;

			if (this.Bounds.IsNull)
			{
				x1 = y1 = z1 = Int32.MaxValue;
				x2 = y2 = z2 = Int32.MinValue;
			}
			else
			{
				x1 = this.Bounds.X1;
				x2 = this.Bounds.X2;
				y1 = this.Bounds.Y1;
				y2 = this.Bounds.Y2;
				z1 = this.Bounds.Z1;
				z2 = this.Bounds.Z2;
			}

			x1 = Math.Min(x1, bounds.X1);
			x2 = Math.Max(x2, bounds.X2);
			y1 = Math.Min(y1, bounds.Y1);
			y2 = Math.Max(y2, bounds.Y2);
			z1 = Math.Min(z1, bounds.Z1);
			z2 = Math.Max(z2, bounds.Z2);

			this.Bounds = new IntCuboid(x1, y1, z1, x2 - x1, y2 - y1, z2 - z1);

			var iter = tileDataList.GetEnumerator();
			foreach (IntPoint3D p in bounds.Range())
			{
				iter.MoveNext();
				TileData data = iter.Current;
				m_tileGrid.SetTileData(p, data);

				if (MapTileChanged != null)
					MapTileChanged(p);
			}
		}


		public void AddStockpile(Stockpile stockpile)
		{
			Debug.Assert(m_stockpiles.All(s => (s.Area.IntersectsWith(stockpile.Area)) == false));

			this.Version++;

			m_stockpiles.Add(stockpile);
		}

		public void RemoveStockpile(Stockpile stockpile)
		{
			this.Version++;
			m_stockpiles.Remove(stockpile);
		}

		public Stockpile GetStockpileAt(IntPoint3D p)
		{
			return m_stockpiles.SingleOrDefault(s => s.Area.Contains(p));
		}


		public void AddBuilding(BuildingObject building)
		{
			Debug.Assert(m_buildings.All(b => (b.Area.IntersectsWith(building.Area)) == false));

			this.Version += 1;

			m_buildings.Add(building);
		}

		public BuildingObject GetBuildingAt(IntPoint3D p)
		{
			return m_buildings.SingleOrDefault(b => b.Area.Contains(p));
		}

		static IList<ClientGameObject> EmptyObjectList = new ClientGameObject[0];

		public IList<ClientGameObject> GetContents(IntPoint3D l)
		{
			List<ClientGameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public IList<ClientGameObject> GetContents()
		{
			return m_objectList.AsReadOnly();
		}

		public ClientGameObject GetFirstObject(IntPoint3D l)
		{
			List<ClientGameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return null;

			return obs.FirstOrDefault();
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

			if (MapTileChanged != null)
				MapTileChanged(l);
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

			if (MapTileChanged != null)
				MapTileChanged(l);
		}

		// called from object when its visual property changes
		internal void OnObjectVisualChanged(ClientGameObject ob)
		{
			if (MapTileChanged != null)
				MapTileChanged(ob.Location);
		}

		public override string ToString()
		{
			return String.Format("Env({0})", this.ObjectID.Value);
		}

		int AStar.IAStarEnvironment.GetTileWeight(IntPoint3D p)
		{
			return 0;
		}

		IEnumerable<Direction> AStar.IAStarEnvironment.GetValidDirs(IntPoint3D p)
		{
			return EnvironmentHelpers.GetDirectionsFrom(this, p);
		}

		bool Dwarrowdelf.AStar.IAStarEnvironment.CanEnter(IntPoint3D p)
		{
			if (!this.Bounds.Contains(p))
				return false;

			var dstInter = GetInterior(p);
			var dstFloor = GetFloor(p);

			return !dstInter.Blocker && dstFloor.IsCarrying;
		}

		void Dwarrowdelf.AStar.IAStarEnvironment.Callback(IDictionary<IntPoint3D, Dwarrowdelf.AStar.AStarNode> nodes)
		{
		}
	}
}
