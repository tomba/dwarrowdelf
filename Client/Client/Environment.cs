using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	enum MapTileObjectChangeType
	{
		Add,
		Remove,
	}

	class Environment : GameObject, IEnvironment
	{
		public event Action<GameObject, IntPoint3D, MapTileObjectChangeType> MapTileObjectChanged;
		public event Action<IntPoint3D> MapTileTerrainChanged;

		GrowingTileGrid m_tileGrid;
		Dictionary<IntPoint3D, List<GameObject>> m_objectMap;
		List<GameObject> m_objectList;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; set; }

		public IntCuboid Bounds { get; set; }

		ObservableCollection<IDrawableElement> m_mapElements;
		public ReadOnlyObservableCollection<IDrawableElement> MapElements { get; private set; }

		public IntPoint3D HomeLocation { get; set; }

		public Designation Designations { get; private set; }

		public Environment(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.Version = 1;

			m_tileGrid = new GrowingTileGrid();
			m_objectMap = new Dictionary<IntPoint3D, List<GameObject>>();
			m_objectList = new List<GameObject>();

			m_mapElements = new ObservableCollection<IDrawableElement>();
			this.MapElements = new ReadOnlyObservableCollection<IDrawableElement>(m_mapElements);

			this.Designations = new Designation(this);

			this.World.AddEnvironment(this);
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			throw new NotImplementedException();
		}

		[Serializable]
		class EnvironmentSave
		{
			public Designation Designation;
		}

		public override object Save()
		{
			return new EnvironmentSave()
			{
				Designation = this.Designations,
			};
		}

		public override void Restore(object data)
		{
			var save = (EnvironmentSave)data;

			this.Designations = save.Designation;
		}

		public bool Contains(IntPoint3D p)
		{
			return this.Bounds.Contains(p);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return GetInterior(l).IsBlocker == false;
		}

		public TerrainID GetTerrainID(IntPoint3D l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public MaterialID GetTerrainMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetTerrainMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public TerrainInfo GetTerrain(IntPoint3D l)
		{
			return Terrains.GetTerrain(GetTerrainID(l));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3D l)
		{
			return Materials.GetMaterial(m_tileGrid.GetTerrainMaterialID(l));
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

			if (MapTileTerrainChanged != null)
				MapTileTerrainChanged(l);
		}

		public void SetTerrainID(IntPoint3D l, TerrainID terrainID)
		{
			this.Version += 1;

			m_tileGrid.SetTerrainID(l, terrainID);

			if (MapTileTerrainChanged != null)
				MapTileTerrainChanged(l);
		}

		public byte GetWaterLevel(IntPoint3D l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetGrass(IntPoint3D ml)
		{
			return m_tileGrid.GetGrass(ml);
		}

		public bool GetHidden(IntPoint3D ml)
		{
			return m_tileGrid.GetTerrainID(ml) == TerrainID.Undefined;
		}

		public TileData GetTileData(IntPoint3D p)
		{
			return m_tileGrid.GetTileData(p);
		}

		public void SetTileData(IntPoint3D l, TileData tileData)
		{
			this.Version += 1;

			m_tileGrid.SetTileData(l, tileData);

			if (MapTileTerrainChanged != null)
				MapTileTerrainChanged(l);
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

				if (MapTileTerrainChanged != null)
					MapTileTerrainChanged(p);
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

				if (MapTileTerrainChanged != null)
					MapTileTerrainChanged(p);
			}
		}


		static IList<GameObject> EmptyObjectList = new GameObject[0];

		public IEnumerable<IGameObject> GetContents(IntRectZ rect)
		{
			return m_objectMap.Where(kvp => rect.Contains(kvp.Key)).SelectMany(kvp => kvp.Value);
		}

		public IList<GameObject> GetContents(IntPoint3D l)
		{
			List<GameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public IList<GameObject> GetContents()
		{
			return m_objectList.AsReadOnly();
		}

		public GameObject GetFirstObject(IntPoint3D l)
		{
			List<GameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return null;

			return obs.FirstOrDefault();
		}

		protected override void ChildAdded(GameObject child)
		{
			IntPoint3D l = child.Location;

			List<GameObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs))
			{
				obs = new List<GameObject>();
				m_objectMap[l] = obs;
			}

			if (child.IsLiving)
				obs.Insert(0, child);
			else
				obs.Add(child);

			m_objectList.Add(child);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, l, MapTileObjectChangeType.Add);
		}

		protected override void ChildRemoved(GameObject child)
		{
			IntPoint3D l = child.Location;

			Debug.Assert(m_objectMap.ContainsKey(l));

			List<GameObject> obs = m_objectMap[l];

			bool removed = obs.Remove(child);
			Debug.Assert(removed);

			removed = m_objectList.Remove(child);
			Debug.Assert(removed);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, l, MapTileObjectChangeType.Remove);
		}

		protected override void ChildMoved(GameObject child, IntPoint3D from, IntPoint3D to)
		{
			List<GameObject> obs;

			/* first remove from the old position ... */

			Debug.Assert(m_objectMap.ContainsKey(from));

			obs = m_objectMap[from];

			bool removed = obs.Remove(child);
			Debug.Assert(removed);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, from, MapTileObjectChangeType.Remove);

			/* ... and then add to the new one */

			if (!m_objectMap.TryGetValue(to, out obs))
			{
				obs = new List<GameObject>();
				m_objectMap[to] = obs;
			}

			if (child.IsLiving)
				obs.Insert(0, child);
			else
				obs.Add(child);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, to, MapTileObjectChangeType.Add);
		}

		// called from object when its visual property changes
		internal void OnObjectVisualChanged(GameObject ob)
		{
			if (MapTileObjectChanged != null)
			{
				// XXX
				MapTileObjectChanged(ob, ob.Location, MapTileObjectChangeType.Remove);
				MapTileObjectChanged(ob, ob.Location, MapTileObjectChangeType.Add);
			}
		}

		public override string ToString()
		{
			return String.Format("Env({0:x})", this.ObjectID.Value);
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
			return EnvironmentHelpers.CanEnter(this, p);
		}

		void Dwarrowdelf.AStar.IAStarEnvironment.Callback(IDictionary<IntPoint3D, Dwarrowdelf.AStar.AStarNode> nodes)
		{
		}


		// XXX
		public Stockpile GetStockpileAt(IntPoint3D p)
		{
			return m_mapElements.OfType<Stockpile>().SingleOrDefault(s => s.Area.Contains(p));
		}

		public BuildingObject GetBuildingAt(IntPoint3D p)
		{
			return m_mapElements.OfType<BuildingObject>().SingleOrDefault(b => b.Area.Contains(p));
		}

		public void AddMapElement(IDrawableElement element)
		{
			this.Version++;

			// XXX can the elements overlap?
			Debug.Assert(m_mapElements.All(s => (s.Area.IntersectsWith(element.Area)) == false));
			Debug.Assert(!m_mapElements.Contains(element));
			m_mapElements.Add(element);
		}

		public void RemoveMapElement(IDrawableElement element)
		{
			this.Version++;

			var ok = m_mapElements.Remove(element);
			Debug.Assert(ok);
		}

		public IDrawableElement GetElementAt(IntPoint3D p)
		{
			return m_mapElements.FirstOrDefault(e => e.Area.Contains(p));
		}

		public IEnumerable<IDrawableElement> GetElementsAt(IntPoint3D p)
		{
			return m_mapElements.Where(e => e.Area.Contains(p));
		}
	}
}
