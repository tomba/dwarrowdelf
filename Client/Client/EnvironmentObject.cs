using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	enum MapTileObjectChangeType
	{
		Add,
		Remove,
		Update,
	}

	[SaveGameObjectByRef(ClientObject = true)]
	sealed class EnvironmentObject : ContainerObject, IEnvironmentObject
	{
		public event Action<MovableObject, IntPoint3, MapTileObjectChangeType> MapTileObjectChanged;
		public event Action<IntPoint3> MapTileTerrainChanged;

		GrowingTileGrid m_tileGrid;
		Dictionary<IntPoint3, List<MovableObject>> m_objectMap;
		List<MovableObject> m_objectList;

		public event Action<MovableObject> ObjectAdded;
		public event Action<MovableObject> ObjectRemoved;
		public event Action<MovableObject, IntPoint3> ObjectMoved;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }

		public IntSize3 Size { get { return m_tileGrid.Size; } }

		[SaveGameProperty(UseOldList = true)]
		ObservableCollection<IAreaElement> m_areaElements;
		public ReadOnlyObservableCollection<IAreaElement> AreaElements { get; private set; }

		[SaveGameProperty]
		public Designation Designations { get; private set; }

		[SaveGameProperty]
		public InstallFurnitureManager InstallFurnitureManager { get; private set; }

		[SaveGameProperty]
		public ConstructManager ConstructManager { get; private set; }

		public ItemTracker ItemTracker { get; private set; }

		public EnvironmentObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.Version = 1;

			m_tileGrid = new GrowingTileGrid();
			m_objectMap = new Dictionary<IntPoint3, List<MovableObject>>();
			m_objectList = new List<MovableObject>();

			m_areaElements = new ObservableCollection<IAreaElement>();
			this.AreaElements = new ReadOnlyObservableCollection<IAreaElement>(m_areaElements);

			this.Designations = new Designation(this);
			this.InstallFurnitureManager = new InstallFurnitureManager(this);
			this.ConstructManager = new ConstructManager(this);

			this.ItemTracker = new ItemTracker(this);

			this.World.AddEnvironment(this);
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (MapData)_data;

			base.Deserialize(_data);

			if (!data.Size.IsEmpty)
				m_tileGrid.SetSize(data.Size);

			this.VisibilityMode = data.VisibilityMode;
		}

		[OnSaveGameDeserialized]
		void OnDeserialized()
		{
			this.AreaElements = new ReadOnlyObservableCollection<IAreaElement>(m_areaElements);
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			throw new NotImplementedException();
		}

		public bool Contains(IntPoint3 p)
		{
			return this.Size.Contains(p);
		}

		public bool IsWalkable(IntPoint3 l)
		{
			return GetInterior(l).IsBlocker == false;
		}

		public TerrainID GetTerrainID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetTerrainMaterialID(l);
		}

		public InteriorID GetInteriorID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public TerrainInfo GetTerrain(IntPoint3 l)
		{
			return Terrains.GetTerrain(GetTerrainID(l));
		}

		public MaterialInfo GetTerrainMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetTerrainMaterialID(l));
		}

		public InteriorInfo GetInterior(IntPoint3 l)
		{
			return Interiors.GetInterior(GetInteriorID(l));
		}

		public MaterialInfo GetInteriorMaterial(IntPoint3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetInteriorMaterialID(l));
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public TileFlags GetTileFlags(IntPoint3 l)
		{
			return m_tileGrid.GetFlags(l);
		}

		public bool GetTileFlags(IntPoint3 l, TileFlags flags)
		{
			return (m_tileGrid.GetFlags(l) & flags) != 0;
		}


		public bool GetHidden(IntPoint3 ml)
		{
			return m_tileGrid.GetTerrainID(ml) == TerrainID.Undefined;
		}

		public TileData GetTileData(IntPoint3 p)
		{
			return m_tileGrid.GetTileData(p);
		}

		public void SetTileData(IntPoint3 l, TileData tileData)
		{
			this.Version += 1;

			m_tileGrid.SetTileData(l, tileData);

			if (MapTileTerrainChanged != null)
				MapTileTerrainChanged(l);
		}

		public void SetTerrains(Tuple<IntPoint3, TileData>[] tileDataList)
		{
			this.Version += 1;

			foreach (var kvp in tileDataList)
			{
				IntPoint3 p = kvp.Item1;
				TileData data = kvp.Item2;

				m_tileGrid.SetTileData(p, data);

				if (MapTileTerrainChanged != null)
					MapTileTerrainChanged(p);
			}
		}

		void ReadAndSetTileData(Stream stream, IntBox bounds)
		{
			using (var reader = new BinaryReader(stream))
			{
				foreach (IntPoint3 p in bounds.Range())
				{
					TileData td = new TileData();
					td.Raw = reader.ReadUInt64();
					m_tileGrid.SetTileData(p, td);

					if (MapTileTerrainChanged != null)
						MapTileTerrainChanged(p);
				}
			}
		}

		public void SetTerrains(IntBox bounds, byte[] tileDataList, bool isCompressed)
		{
			this.Version += 1;

			m_tileGrid.Grow(bounds.Corner2);

			//Trace.TraceError("Recv {0}", bounds.Z);

			using (var memStream = new MemoryStream(tileDataList))
			{
				if (isCompressed == false)
				{
					ReadAndSetTileData(memStream, bounds);
				}
				else
				{
					using (var decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
						ReadAndSetTileData(decompressStream, bounds);
				}
			}
#if asd
			Task.Factory.StartNew(() =>
			{
				var dstStream = new MemoryStream();

				using (var memStream = new MemoryStream(tileDataList))
				using (var decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
					decompressStream.CopyTo(dstStream);

				dstStream.Position = 0;
				return dstStream;
			}).ContinueWith(t =>
			{
				var stream = t.Result;

				using (var streamReader = new BinaryReader(stream))
				{
					TileData td = new TileData();
					foreach (IntPoint3 p in bounds.Range())
					{
						td.Raw = streamReader.ReadUInt64();
						m_tileGrid.SetTileData(p, td);

						if (MapTileTerrainChanged != null)
							MapTileTerrainChanged(p);
					}
				}

				stream.Dispose();

				//Trace.TraceError("done {0}", bounds.Z);

			}, TaskScheduler.FromCurrentSynchronizationContext());

#endif
		}


		static IList<MovableObject> EmptyObjectList = new MovableObject[0];

		public IEnumerable<IMovableObject> GetContents(IntRectZ rect)
		{
			return m_objectMap.Where(kvp => rect.Contains(kvp.Key)).SelectMany(kvp => kvp.Value);
		}

		IEnumerable<IMovableObject> IEnvironmentObject.GetContents(IntPoint3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public IList<MovableObject> GetContents(IntPoint3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public IList<MovableObject> GetContents()
		{
			return m_objectList.AsReadOnly();
		}

		public MovableObject GetFirstObject(IntPoint3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return null;

			return obs.FirstOrDefault();
		}

		protected override void ChildAdded(MovableObject child)
		{
			IntPoint3 l = child.Location;

			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs))
			{
				obs = new List<MovableObject>();
				m_objectMap[l] = obs;
			}

			Debug.Assert(!obs.Contains(child));

			if (child.IsLiving)
				obs.Insert(0, child);
			else
				obs.Add(child);

			Debug.Assert(!m_objectList.Contains(child));
			m_objectList.Add(child);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, l, MapTileObjectChangeType.Add);

			if (this.ObjectAdded != null)
				this.ObjectAdded(child);
		}

		protected override void ChildRemoved(MovableObject child)
		{
			IntPoint3 l = child.Location;

			Debug.Assert(m_objectMap.ContainsKey(l));

			List<MovableObject> obs = m_objectMap[l];

			bool removed = obs.Remove(child);
			Debug.Assert(removed);

			removed = m_objectList.Remove(child);
			Debug.Assert(removed);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, l, MapTileObjectChangeType.Remove);

			if (this.ObjectRemoved != null)
				this.ObjectRemoved(child);
		}

		protected override void ChildMoved(MovableObject child, IntPoint3 from, IntPoint3 to)
		{
			List<MovableObject> obs;

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
				obs = new List<MovableObject>();
				m_objectMap[to] = obs;
			}

			Debug.Assert(!obs.Contains(child));

			if (child.IsLiving)
				obs.Insert(0, child);
			else
				obs.Add(child);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, to, MapTileObjectChangeType.Add);

			if (this.ObjectMoved != null)
				this.ObjectMoved(child, from);
		}

		// called from object when its visual property changes
		internal void OnObjectVisualChanged(MovableObject ob)
		{
			if (MapTileObjectChanged != null)
				MapTileObjectChanged(ob, ob.Location, MapTileObjectChangeType.Update);
		}

		public override string ToString()
		{
			return String.Format("Env({0:x})", this.ObjectID.Value);
		}

		int AStar.IAStarEnvironment.GetTileWeight(IntPoint3 p)
		{
			return 0;
		}

		IEnumerable<Direction> AStar.IAStarEnvironment.GetValidDirs(IntPoint3 p)
		{
			return EnvironmentHelpers.GetDirectionsFrom(this, p);
		}

		bool AStar.IAStarEnvironment.CanEnter(IntPoint3 p)
		{
			return EnvironmentHelpers.CanEnter(this, p);
		}

		void AStar.IAStarEnvironment.Callback(IDictionary<IntPoint3, AStar.AStarNode> nodes)
		{
		}


		public void AddAreaElement(IAreaElement element)
		{
			this.Version++;

			Debug.Assert(!m_areaElements.Contains(element));
			// XXX when constructing a building, there's a construction site at the location of the building
			//Debug.Assert(m_areaElements.All(s => (s.Area.IntersectsWith(element.Area)) == false));
			m_areaElements.Add(element);
		}

		public void RemoveAreaElement(IAreaElement element)
		{
			this.Version++;

			var ok = m_areaElements.Remove(element);
			Debug.Assert(ok);
		}

		public IAreaElement GetElementAt(IntPoint3 p)
		{
			return m_areaElements.FirstOrDefault(e => e.Area.Contains(p));
		}
	}
}
