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
	public enum MapTileObjectChangeType
	{
		Add,
		Remove,
		Update,
	}

	[SaveGameObject(ClientObject = true)]
	public sealed class EnvironmentObject : ContainerObject, IEnvironmentObject, ISaveGameDelegate
	{
		[Serializable]
		sealed class EnvironmentObjectClientData
		{
			public ObservableCollection<IAreaElement> AreaElements;
			public Designation Designations;
			public InstallItemManager InstallItemManager;
			public ConstructManager ConstructManager;
		}

		/// <summary>
		/// Object in a tile has moved
		/// </summary>
		public event Action<MovableObject, IntPoint3, MapTileObjectChangeType> MapTileObjectChanged;

		/// <summary>
		/// Tile's terrain data changed
		/// </summary>
		public event Action<IntPoint3> MapTileTerrainChanged;

		/// <summary>
		/// Extra visual data for tile changed (designation, ...)
		/// </summary>
		public event Action<IntPoint3> MapTileExtraChanged;

		GrowingTileGrid m_tileGrid;
		Dictionary<IntPoint3, List<MovableObject>> m_objectMap;

		public event Action<MovableObject> ObjectAdded;
		public event Action<MovableObject> ObjectRemoved;
		public event Action<MovableObject, IntPoint3> ObjectMoved;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }

		public IntSize3 Size { get { return m_tileGrid.Size; } }

		ObservableCollection<IAreaElement> m_areaElements;
		public ReadOnlyObservableCollection<IAreaElement> AreaElements { get; private set; }

		public ItemTracker ItemTracker { get; private set; }

		public EnvironmentObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.Version = 1;

			m_tileGrid = new GrowingTileGrid();
			m_objectMap = new Dictionary<IntPoint3, List<MovableObject>>();

			m_areaElements = new ObservableCollection<IAreaElement>();
			this.AreaElements = new ReadOnlyObservableCollection<IAreaElement>(m_areaElements);

			this.ItemTracker = new ItemTracker(this);

			this.Designations = new Designation(this);
			this.InstallItemManager = new InstallItemManager(this);
			this.ConstructManager = new ConstructManager(this);
		}

		public override void Destruct()
		{
			base.Destruct();
		}

		public override void ReceiveObjectData(BaseGameObjectData _data)
		{
			var data = (EnvironmentObjectData)_data;

			base.ReceiveObjectData(_data);

			// XXX we currently always get the map size
			if (!data.Size.IsEmpty)
				m_tileGrid.SetSize(data.Size);

			this.VisibilityMode = data.VisibilityMode;
		}

		object ISaveGameDelegate.GetSaveData()
		{
			return new EnvironmentObjectClientData()
			{
				AreaElements = m_areaElements,
				Designations = this.Designations,
				InstallItemManager = this.InstallItemManager,
				ConstructManager = this.ConstructManager,
			};
		}

		void ISaveGameDelegate.RestoreSaveData(object _data)
		{
			var data = (EnvironmentObjectClientData)_data;

			m_areaElements = data.AreaElements;
			this.AreaElements = new ReadOnlyObservableCollection<IAreaElement>(m_areaElements);

			this.Designations = data.Designations;
			this.InstallItemManager = data.InstallItemManager;
			this.ConstructManager = data.ConstructManager;
		}

		Designation m_designations;
		public Designation Designations
		{
			get { return m_designations; }

			private set
			{
				if (m_designations != null)
					m_designations.Unregister();

				m_designations = value;

				if (m_designations != null)
					m_designations.Register();
			}
		}

		InstallItemManager m_installItemManager;
		public InstallItemManager InstallItemManager
		{
			get { return m_installItemManager; }

			private set
			{
				if (m_installItemManager != null)
					m_installItemManager.Unregister();

				m_installItemManager = value;

				if (m_installItemManager != null)
					m_installItemManager.Register();
			}
		}

		ConstructManager m_constructManager;
		public ConstructManager ConstructManager
		{
			get { return m_constructManager; }

			private set
			{
				if (m_constructManager != null)
					m_constructManager.Unregister();

				m_constructManager = value;

				if (m_constructManager != null)
					m_constructManager.Register();
			}
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

			int x, y, z;
			x = y = z = 0;

			foreach (var kvp in tileDataList)
			{
				IntPoint3 p = kvp.Item1;

				if (x < p.X)
					x = p.X;
				if (y < p.Y)
					y = p.Y;
				if (z < p.Z)
					z = p.Z;
			}

			m_tileGrid.Grow(new IntPoint3(x, y, z));

			foreach (var kvp in tileDataList)
			{
				IntPoint3 p = kvp.Item1;
				TileData data = kvp.Item2;

				m_tileGrid.SetTileData(p, data);

				if (MapTileTerrainChanged != null)
					MapTileTerrainChanged(p);
			}
		}

		void ReadAndSetTileData(Stream stream, IntGrid3 bounds)
		{
			using (var reader = new BinaryReader(stream))
			{
				TileData td = new TileData();

				foreach (IntPoint3 p in bounds.Range())
				{
					td.Raw = reader.ReadUInt64();

					m_tileGrid.SetTileData(p, td);

					if (MapTileTerrainChanged != null)
						MapTileTerrainChanged(p);
				}
			}
		}

		public void SetTerrains(IntGrid3 bounds, byte[] tileDataList, bool isCompressed)
		{
			this.Version += 1;

			m_tileGrid.Grow(bounds.Corner2);

			//Trace.TraceError("Recv {0}", bounds.Z);

#if !parallel
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
#else
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
				using (var stream = t.Result)
					ReadAndSetTileData(stream, bounds);

				//Trace.TraceError("done {0}", bounds.Z);

			}, TaskScheduler.FromCurrentSynchronizationContext());

#endif
		}


		static IList<MovableObject> EmptyObjectList = new MovableObject[0];

		public IEnumerable<IMovableObject> GetContents(IntGrid2Z rect)
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

		internal void OnTileExtraChanged(IntPoint3 p)
		{
			if (this.MapTileExtraChanged != null)
				this.MapTileExtraChanged(p);
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
			Debug.Assert(m_areaElements.All(s => (s.Area.IntersectsWith(element.Area)) == false));
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
