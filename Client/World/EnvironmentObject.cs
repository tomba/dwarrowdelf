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
		public event Action<MovableObject, IntVector3, MapTileObjectChangeType> MapTileObjectChanged;

		/// <summary>
		/// Tile's terrain data changed
		/// </summary>
		public event Action<IntVector3> MapTileTerrainChanged;

		/// <summary>
		/// Extra visual data for tile changed (designation, ...)
		/// </summary>
		public event Action<IntVector3> MapTileExtraChanged;

		GrowingTileGrid m_tileGrid;
		Dictionary<IntVector3, List<MovableObject>> m_objectMap;

		public event Action<MovableObject> ObjectAdded;
		public event Action<MovableObject> ObjectRemoved;
		public event Action<MovableObject, IntVector3> ObjectMoved;

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
			m_objectMap = new Dictionary<IntVector3, List<MovableObject>>();

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
			foreach (var element in m_areaElements)
				element.Register();

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

		public bool Contains(IntVector3 p)
		{
			return this.Size.Contains(p);
		}

		public TileID GetTileID(IntVector3 l)
		{
			return m_tileGrid.GetTileID(l);
		}

		public MaterialID GetMaterialID(IntVector3 l)
		{
			return m_tileGrid.GetMaterialID(l);
		}

		public MaterialInfo GetMaterial(IntVector3 l)
		{
			return Materials.GetMaterial(m_tileGrid.GetMaterialID(l));
		}

		public byte GetWaterLevel(IntVector3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public TileFlags GetTileFlags(IntVector3 l)
		{
			return m_tileGrid.GetFlags(l);
		}

		public bool GetTileFlags(IntVector3 l, TileFlags flags)
		{
			return (m_tileGrid.GetFlags(l) & flags) != 0;
		}


		public bool GetHidden(IntVector3 ml)
		{
			return m_tileGrid.GetTileID(ml) == TileID.Undefined;
		}

		public TileData GetTileData(IntVector3 p)
		{
			return m_tileGrid.GetTileData(p);
		}

		public void SetTileData(IntVector3 l, TileData tileData)
		{
			this.Version += 1;

			m_tileGrid.SetTileData(l, tileData);

			if (MapTileTerrainChanged != null)
				MapTileTerrainChanged(l);
		}

		public void SetTerrains(KeyValuePair<IntVector3, TileData>[] tileDataList)
		{
			this.Version += 1;

			int x, y, z;
			x = y = z = 0;

			foreach (var kvp in tileDataList)
			{
				IntVector3 p = kvp.Key;

				if (x < p.X)
					x = p.X;
				if (y < p.Y)
					y = p.Y;
				if (z < p.Z)
					z = p.Z;
			}

			m_tileGrid.Grow(new IntVector3(x, y, z));

			foreach (var kvp in tileDataList)
			{
				IntVector3 p = kvp.Key;
				TileData data = kvp.Value;

				m_tileGrid.SetTileData(p, data);

				if (MapTileTerrainChanged != null)
					MapTileTerrainChanged(p);
			}
		}

		public void SetTerrains(IntGrid3 bounds, ulong[] tileData)
		{
			this.Version += 1;

			m_tileGrid.Grow(bounds.Corner2);

			//Trace.TraceError("Recv {0}", bounds.Z);

			m_tileGrid.SetTileDataRange(tileData, bounds);

			if (this.MapTileTerrainChanged != null)
			{
				foreach (var p in bounds.Range())
					MapTileTerrainChanged(p);
			}
		}


		static IList<MovableObject> EmptyObjectList = new MovableObject[0];

		public IEnumerable<IMovableObject> GetContents(IntGrid2Z rect)
		{
			return m_objectMap.Where(kvp => rect.Contains(kvp.Key)).SelectMany(kvp => kvp.Value);
		}

		IEnumerable<IMovableObject> IEnvironmentObject.GetContents(IntVector3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public IList<MovableObject> GetContents(IntVector3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return EmptyObjectList;

			return obs.AsReadOnly();
		}

		public MovableObject GetFirstObject(IntVector3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return null;

			return obs.FirstOrDefault();
		}

		public bool HasContents(IntVector3 l)
		{
			List<MovableObject> obs;
			if (!m_objectMap.TryGetValue(l, out obs) || obs == null)
				return false;

			return obs.Count > 0;
		}

		protected override void ChildAdded(MovableObject child)
		{
			IntVector3 l = child.Location;

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
			IntVector3 l = child.Location;

			Debug.Assert(m_objectMap.ContainsKey(l));

			List<MovableObject> obs = m_objectMap[l];

			bool removed = obs.Remove(child);
			Debug.Assert(removed);

			if (MapTileObjectChanged != null)
				MapTileObjectChanged(child, l, MapTileObjectChangeType.Remove);

			if (this.ObjectRemoved != null)
				this.ObjectRemoved(child);
		}

		protected override void ChildMoved(MovableObject child, IntVector3 from, IntVector3 to)
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

		internal void OnTileExtraChanged(IntVector3 p)
		{
			if (this.MapTileExtraChanged != null)
				this.MapTileExtraChanged(p);
		}

		public override string ToString()
		{
			return String.Format("Env({0:x})", this.ObjectID.Value);
		}

		public void AddAreaElement(IAreaElement element)
		{
			this.Version++;

			Debug.Assert(!m_areaElements.Contains(element));
			Debug.Assert(m_areaElements.All(s => (s.Area.IntersectsWith(element.Area)) == false));
			m_areaElements.Add(element);
			element.Register();
		}

		public void RemoveAreaElement(IAreaElement element)
		{
			this.Version++;

			element.Unregister();
			var ok = m_areaElements.Remove(element);
			Debug.Assert(ok);
		}

		public IAreaElement GetElementAt(IntVector3 p)
		{
			return m_areaElements.FirstOrDefault(e => e.Area.Contains(p));
		}
	}
}
