using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Server
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

		public IntCuboid Bounds
		{
			get { return new IntCuboid(0, 0, 0, this.Width, this.Height, this.Depth); }
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


		HashSet<IntPoint3D> m_waterTiles = new HashSet<IntPoint3D>();

		public void ScanWaterTiles()
		{
			foreach (var p in this.Bounds.Range())
			{
				if (m_tileGrid.GetWaterLevel(p) > 0)
					m_waterTiles.Add(p);
				else
					m_waterTiles.Remove(p);
			}
		}

		// XXX bad shuffle
		static Random s_waterRandom = new Random();
		static void ShuffleArray(Direction[] array)
		{
			if (array.Length == 0)
				return;

			for (int i = array.Length - 1; i >= 0; i--)
			{
				var tmp = array[i];
				int randomIndex = s_waterRandom.Next(i + 1);

				//Swap elements
				array[i] = array[randomIndex];
				array[randomIndex] = tmp;
			}
		}

		/// <summary>
		/// Divide signed integer m with positive integer n, rounding up for positive m and and down for negative m
		/// </summary>
		/// <param name="m">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Quotient</returns>
		static int IntDivRound(int m, int n)
		{
			return (m + (m >= 0 ? (n - 1) : -(n - 1))) / n;
		}

		/// <summary>
		/// Clamp an integer between two values
		/// </summary>
		/// <param name="value">Value to be clamped</param>
		/// <param name="max">Maximum value</param>
		/// <param name="min">Minimum value</param>
		/// <returns>Clamped value</returns>
		static int IntClamp(int value, int max, int min)
		{
			return value > max ? max : (value < min ? min : value);
		}

		void HandleWaterAt(IntPoint3D p, Dictionary<IntPoint3D, int> waterChangeMap)
		{
			int curLevel;

			if (!waterChangeMap.TryGetValue(p, out curLevel))
			{
				curLevel = m_tileGrid.GetWaterLevel(p);
				/* water evaporates */
				/*
				if (curLevel == 1 && s_waterRandom.Next(100) == 0)
				{
					waterChangeMap[p] = 0;
					return;
				}
				 */
			}

			var dirs = DirectionExtensions.CardinalDirections.ToArray();
			ShuffleArray(dirs);
			bool curLevelChanged = false;

			for (int i = 0; i < dirs.Length; ++i)
			{
				var pp = p + dirs[i];

				if (!this.Bounds.Contains(pp) || !this.IsWalkable(pp))
					continue;

				int neighLevel;
				if (!waterChangeMap.TryGetValue(pp, out neighLevel))
					neighLevel = m_tileGrid.GetWaterLevel(pp);

				int diff = curLevel - neighLevel;
				int flow = IntDivRound(diff, 6);
				flow = IntClamp(flow, curLevel > 1 ? curLevel - 1 : 0, neighLevel > 1 ? -neighLevel + 1 : 0);

				if (flow == 0)
					continue;

				curLevel -= flow;
				neighLevel += flow;

				waterChangeMap[pp] = neighLevel;
				curLevelChanged = true;
			}

			if (curLevelChanged)
				waterChangeMap[p] = curLevel;
		}

		void HandleWater()
		{
			IntPoint3D[] waterTiles = m_waterTiles.ToArray();
			Dictionary<IntPoint3D, int> waterChangeMap = new Dictionary<IntPoint3D, int>();

			foreach (var p in waterTiles)
			{
				if (m_tileGrid.GetWaterLevel(p) == 0)
					throw new Exception();

				HandleWaterAt(p, waterChangeMap);
			}

			foreach (var kvp in waterChangeMap)
			{
				var p = kvp.Key;
				int level = kvp.Value;

				var td = m_tileGrid.GetTileData(p);
				td.WaterLevel = (byte)level;
				m_tileGrid.SetWaterLevel(p, (byte)level);

				if (MapChanged != null)
					MapChanged(this, p, td);

				if (level > 0)
					m_waterTiles.Add(p);
				else
					m_waterTiles.Remove(p);
			}
		}

		public void Tick()
		{
			HandleWater();
		}

		public InteriorInfo GetInterior(IntPoint3D p)
		{
			return Interiors.GetInterior(GetInteriorID(p));
		}

		public InteriorID GetInteriorID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorID(l);
		}

		public MaterialID GetInteriorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetInteriorMaterialID(l);
		}

		public FloorInfo GetFloor(IntPoint3D p)
		{
			return Floors.GetFloor(GetFloorID(p));
		}

		public FloorID GetFloorID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorID(l);
		}

		public MaterialID GetFloorMaterialID(IntPoint3D l)
		{
			return m_tileGrid.GetFloorMaterialID(l);
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
			return !Interiors.GetInterior(GetInteriorID(l)).Blocker;
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

		public bool CanEnter(IntPoint3D location)
		{
			var dstInter = GetInterior(location);
			var dstFloor = GetFloor(location);

			return !dstInter.Blocker && dstFloor.IsCarrying;
		}

		bool CanMoveTo(IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			IntVector3D v = dstLoc - srcLoc;

			if (!v.IsNormal)
				throw new Exception();

			var dstInter = GetInterior(dstLoc);
			var dstFloor = GetFloor(dstLoc);

			if (dstInter.Blocker || !dstFloor.IsCarrying)
				return false;

			if (v.Z == 0)
				return true;

			Direction dir = v.ToDirection();

			var srcInter = GetInterior(srcLoc);
			var srcFloor = GetFloor(srcLoc);

			if (dir == Direction.Up)
				return srcInter.ID == InteriorID.Stairs && dstFloor.ID == FloorID.Hole;

			if (dir == Direction.Down)
				return dstInter.ID == InteriorID.Stairs && srcFloor.ID == FloorID.Hole;

			var d2d = v.ToIntVector().ToDirection();

			if (dir.ContainsUp())
			{
				var tileAboveSlope = GetTileData(srcLoc + Direction.Up);
				return d2d.IsCardinal() && srcInter.ID.IsSlope() && srcInter.ID == Interiors.GetSlopeFromDir(d2d) && tileAboveSlope.IsEmpty;
			}

			if (dir.ContainsDown())
			{
				var tileAboveSlope = GetTileData(dstLoc + Direction.Up);
				return d2d.IsCardinal() && dstInter.ID.IsSlope() && dstInter.ID == Interiors.GetSlopeFromDir(d2d.Reverse()) && tileAboveSlope.IsEmpty;
			}

			return false;
		}

		protected override bool OkToMoveChild(ServerGameObject ob, Direction dir, IntPoint3D dstLoc)
		{
			if (!this.Bounds.Contains(dstLoc))
				return false;

			return CanMoveTo(ob.Location, dstLoc);
		}

		protected override void OnChildMoved(ServerGameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc)
		{
			if (srcLoc.Z == dstLoc.Z)
				return;

			var list = m_contentArray[srcLoc.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);

			list = m_contentArray[dstLoc.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}


		HashSet<BuildingObject> m_buildings = new HashSet<BuildingObject>();

		public void AddBuilding(BuildingObject building)
		{
			Debug.Assert(this.World.IsWritable);

			Debug.Assert(m_buildings.Any(b => b.Z == building.Z && b.Area.IntersectsWith(building.Area)) == false);
			Debug.Assert(building.Environment == null);
			building.Environment = this;
			m_buildings.Add(building);

			this.World.AddChange(new FullObjectChange(building) { ObjectData = building.Serialize() });
		}

		public BuildingObject GetBuildingAt(IntPoint3D p)
		{
			return m_buildings.SingleOrDefault(b => b.Contains(p));
		}

		public override void SerializeTo(Action<ClientMsgs.Message> writer)
		{
			writer(new ClientMsgs.MapData()
			{
				Environment = this.ObjectID,
				VisibilityMode = this.VisibilityMode,
				Bounds = this.Bounds,
			});

			var bounds = this.Bounds;

			if (bounds.Volume < 2000)
			{
				// Send everything in one message
				var arr = new TileData[bounds.Volume];
				foreach (var p in this.Bounds.Range())
				{
					int idx = bounds.GetIndex(p);
					arr[idx] = m_tileGrid.GetTileData(p);
				}

				writer(new ClientMsgs.MapDataTerrains()
				{
					Environment = this.ObjectID,
					Bounds = bounds,
					TerrainData = arr,
				});
			}
			else if (bounds.Plane.Area < 2000)
			{
				var plane = bounds.Plane;
				// Send every 2D plane in one message
				var arr = new TileData[plane.Area];
				var msg = new ClientMsgs.MapDataTerrains() { Environment = this.ObjectID };

				for (int z = bounds.Z1; z < bounds.Z2; ++z)
				{
					foreach (var p2d in plane.Range())
					{
						int idx = plane.GetIndex(p2d);
						var p = new IntPoint3D(p2d, z);
						arr[idx] = m_tileGrid.GetTileData(p);
					}

					msg.Bounds = new IntCuboid(bounds.X1, bounds.Y1, z, bounds.Width, bounds.Height, 1);
					msg.TerrainData = arr;

					writer(msg);
				}
			}
			else
			{
				// Send every line in one message
				var arr = new TileData[this.Width];
				var msg = new ClientMsgs.MapDataTerrains() { Environment = this.ObjectID };

				for (int z = bounds.Z1; z < bounds.Z2; ++z)
				{
					for (int y = bounds.Y1; y < bounds.Y2; ++y)
					{
						for (int x = bounds.X1; x < bounds.X2; ++x)
						{
							IntPoint3D p = new IntPoint3D(x, y, z);
							arr[x] = m_tileGrid.GetTileData(p);
						}

						msg.Bounds = new IntCuboid(bounds.X1, y, z, bounds.Width, 1, 1);
						msg.TerrainData = arr;

						writer(msg);
					}
				}
			}

			for (int z = bounds.Z1; z < bounds.Z2; ++z)
			{
				var msg = new ClientMsgs.MapDataObjects()
				{
					Environment = this.ObjectID,
					ObjectData = m_contentArray[z].Select(o => o.Serialize()).ToArray(),
				};

				writer(msg);
			}

			// this may not need dividing, perhaps
			writer(new ClientMsgs.MapDataBuildings()
			{
				Environment = this.ObjectID,
				BuildingData = m_buildings.Select(b => (ClientMsgs.BuildingData)b.Serialize()).ToArray(),
			});
		}

		public override ClientMsgs.Message Serialize()
		{
			throw new Exception();
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}


		class TileGrid
		{
			TileData[, ,] m_grid;

			public TileGrid(int width, int height, int depth)
			{
				m_grid = new TileData[depth, height, width];
			}

			public TileData GetTileData(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X];
			}

			public void SetTileData(IntPoint3D p, TileData data)
			{
				m_grid[p.Z, p.Y, p.X] = data;
			}

			public void SetInteriorID(IntPoint3D p, InteriorID id)
			{
				m_grid[p.Z, p.Y, p.X].InteriorID = id;
			}

			public InteriorID GetInteriorID(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X].InteriorID;
			}

			public void SetFloorID(IntPoint3D p, FloorID id)
			{
				m_grid[p.Z, p.Y, p.X].FloorID = id;
			}

			public FloorID GetFloorID(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X].FloorID;
			}


			public void SetInteriorMaterialID(IntPoint3D p, MaterialID id)
			{
				m_grid[p.Z, p.Y, p.X].InteriorMaterialID = id;
			}

			public MaterialID GetInteriorMaterialID(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X].InteriorMaterialID;
			}

			public void SetFloorMaterialID(IntPoint3D p, MaterialID id)
			{
				m_grid[p.Z, p.Y, p.X].FloorMaterialID = id;
			}

			public MaterialID GetFloorMaterialID(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X].FloorMaterialID;
			}

			public void SetWaterLevel(IntPoint3D p, byte waterLevel)
			{
				m_grid[p.Z, p.Y, p.X].WaterLevel = waterLevel;
			}

			public byte GetWaterLevel(IntPoint3D p)
			{
				return m_grid[p.Z, p.Y, p.X].WaterLevel;
			}
		}
	}
}
