using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	public delegate void MapChanged(Environment map, IntPoint3D l, TileData tileData);

	public class Environment : ServerGameObject 
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;
		// XXX this is quite good for add/remove child, but bad for gettings objects at certain location
		KeyedObjectCollection[] m_contentArray;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		public Environment(World world, int width, int height, int depth, VisibilityMode visibilityMode)
			: base(world)
		{
			this.Version = 1;
			base.Name = "unnamed map";
			this.VisibilityMode = visibilityMode;

			this.Width = width;
			this.Height = height;
			this.Depth = depth;

			m_tileGrid = new TileGrid(this.Width, this.Height, this.Depth);
			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();
		}

		public IntRect Bounds2D
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public IntCube Bounds
		{
			get { return new IntCube(0, 0, 0, this.Width, this.Height, this.Depth); }
		}

		public delegate bool ActionHandlerDelegate(ServerGameObject ob, GameAction action);

		Dictionary<IntPoint3D, ActionHandlerDelegate> m_actionHandlers = new Dictionary<IntPoint3D, ActionHandlerDelegate>();
		public void SetActionHandler(IntPoint3D p, ActionHandlerDelegate handler)
		{
			m_actionHandlers[p] = handler;
		}

		public override bool HandleChildAction(ServerGameObject child, GameAction action)
		{
			ActionHandlerDelegate handler;
			if (m_actionHandlers.TryGetValue(child.Location, out handler) == false)
				return false;

			return handler(child, action);
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public FloorID GetFloorID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorID(l);
		}

		public void SetInterior(IntPoint3D p, InteriorID interiorID, MaterialID materialID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetInteriorID(p, interiorID);
			m_tileGrid.SetInteriorMaterialID(p, materialID);

			var d = m_tileGrid.GetTileData(p);

			if (MapChanged != null)
				MapChanged(this, p, d);
		}

		public void SetFloor(IntPoint3D p, FloorID floorID, MaterialID materialID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetFloorID(p, floorID);
			m_tileGrid.SetFloorMaterialID(p, materialID);

			var d = m_tileGrid.GetTileData(p);

			if (MapChanged != null)
				MapChanged(this, p, d);
		}

		public void SetInteriorID(IntPoint3D l, InteriorID interiorID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetInteriorID(l, interiorID);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public void SetFloorID(IntPoint3D l, FloorID floorID)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetFloorID(l, floorID);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public TileData GetTileData(IntPoint3D l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public void SetTileData(IntPoint3D l, TileData data)
		{
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetTileData(l, data);

			var d = m_tileGrid.GetTileData(l);

			if (MapChanged != null)
				MapChanged(this, l, d);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return !this.World.AreaData.Terrains.GetInteriorInfo(GetInteriorID(l)).Blocker;
		}

		// XXX not a good func. contents can be changed by the caller
		public IEnumerable<ServerGameObject> GetContents(IntPoint3D l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		protected override void OnChildAdded(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		protected override void OnChildRemoved(ServerGameObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);
		}

		protected override bool OkToAddChild(ServerGameObject ob, IntPoint3D p)
		{
			Debug.Assert(this.World.IsWritable);

			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			return true;
		}

		protected override bool OkToMoveChild(ServerGameObject ob, IntVector3D dirVec, IntPoint3D p)
		{
			Debug.Assert(this.World.IsWritable);

			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			if (dirVec.Z == 0)
				return true;

			if (dirVec.X != 0 || dirVec.Y != 0)
				return false;

			var tileID = m_tileGrid.GetInteriorID(ob.Location);

			if (tileID == InteriorID.StairsUp && dirVec.Z == 1)
				return true;

			if (tileID == InteriorID.StairsDown && dirVec.Z == -1)
				return true;

			return false;
		}

		protected override void OnChildMoved(ServerGameObject child, IntPoint3D oldLocation, IntPoint3D newLocation)
		{
			if (oldLocation.Z == newLocation.Z)
				return;

			var list = m_contentArray[oldLocation.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);

			list = m_contentArray[newLocation.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		public override ClientMsgs.Message Serialize()
		{
			var arr = new TileData[this.Width * this.Height * this.Depth];
			List<ClientMsgs.Message> obList = new List<ClientMsgs.Message>();

			foreach (var p in this.Bounds.Range())
			{
				TileData d;
				d = m_tileGrid.GetTileData(p);
				arr[p.X + p.Y * this.Width + p.Z * this.Width * this.Height] = d;
				var obs = GetContents(p);
				if (obs != null)
					obList.AddRange(obs.Select(o => o.Serialize()));
			}

			var msg = new ClientMsgs.FullMapData()
			{
				ObjectID = this.ObjectID,
				VisibilityMode = this.VisibilityMode,
				Bounds = this.Bounds,
				TerrainIDs = arr,
				ObjectData = obList,
			};

			return msg;
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}


		class TileGrid : Grid3DBase<TileData>
		{
			public TileGrid(int width, int height, int depth)
				: base(width, height, depth)
			{
			}

			public TileData GetTileData(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)];
			}

			public void SetTileData(IntPoint3D p, TileData data)
			{
				base.Grid[GetIndex(p)] = data;
			}

			public void SetInteriorID(IntPoint3D p, InteriorID id)
			{
				base.Grid[GetIndex(p)].InteriorID = id;
			}

			public InteriorID GetInteriorID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].InteriorID;
			}

			public void SetFloorID(IntPoint3D p, FloorID id)
			{
				base.Grid[GetIndex(p)].FloorID = id;
			}

			public FloorID GetFloorID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].FloorID;
			}


			public void SetInteriorMaterialID(IntPoint3D p, MaterialID id)
			{
				base.Grid[GetIndex(p)].InteriorMaterialID = id;
			}

			public MaterialID GetInteriorMaterialID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].InteriorMaterialID;
			}

			public void SetFloorMaterialID(IntPoint3D p, MaterialID id)
			{
				base.Grid[GetIndex(p)].FloorMaterialID = id;
			}

			public MaterialID GetFloorMaterialID(IntPoint3D p)
			{
				return base.Grid[GetIndex(p)].FloorMaterialID;
			}		
		}

	}
}
