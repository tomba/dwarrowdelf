using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public sealed class EnvironmentObject : ContainerObject, IEnvironmentObject
	{
		internal static EnvironmentObject Create(World world, EnvironmentObjectBuilder builder)
		{
			var ob = new EnvironmentObject(builder);
			ob.Initialize(world);
			return ob;
		}

		[SaveGameProperty("Grid", ReaderWriter = typeof(TileGridReaderWriter))]
		TileGrid m_tileGrid;

		// XXX this is quite good for add/remove child, but bad for gettings objects at certain location
		KeyedObjectCollection[] m_contentArray;

		[SaveGameProperty]
		public uint Version { get; private set; }

		[SaveGameProperty]
		public VisibilityMode VisibilityMode { get; private set; }

		[SaveGameProperty]
		public int Width { get; private set; }
		[SaveGameProperty]
		public int Height { get; private set; }
		[SaveGameProperty]
		public int Depth { get; private set; }

		public IntSize3 Size { get { return new IntSize3(this.Width, this.Height, this.Depth); } }

		[SaveGameProperty("LargeObjects", Converter = typeof(LargeObjectSetConv))]
		HashSet<AreaObject> m_largeObjectSet;

		public event Action<AreaObject> LargeObjectAdded;
		public event Action<AreaObject> LargeObjectRemoved;

		HashSet<IntPoint3> m_waterTiles = new HashSet<IntPoint3>();

		public event Action<IntPoint3, TileData, TileData> TerrainOrInteriorChanged;

		EnvironmentObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Environment)
		{
		}

		EnvironmentObject(EnvironmentObjectBuilder builder)
			: base(ObjectType.Environment)
		{
			this.Version = 1;
			this.VisibilityMode = builder.VisibilityMode;

			m_tileGrid = builder.Grid;
			var size = m_tileGrid.Size;

			this.Width = size.Width;
			this.Height = size.Height;
			this.Depth = size.Depth;

			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < size.Depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();

			m_largeObjectSet = new HashSet<AreaObject>();
		}

		[OnSaveGamePostDeserialization]
		void OnDeserialized()
		{
			m_contentArray = new KeyedObjectCollection[this.Depth];
			for (int i = 0; i < this.Depth; ++i)
				m_contentArray[i] = new KeyedObjectCollection();

			foreach (var ob in this.Inventory)
				m_contentArray[ob.Z].Add(ob);

			ScanWaterTiles();
		}

		protected override void Initialize(World world)
		{
			base.Initialize(world);

			world.TickStarting += Tick;
		}

		public override void Destruct()
		{
			this.World.TickStarting -= Tick;

			base.Destruct();
		}

		void MapChanged(IntPoint3 l, TileData tileData)
		{
			this.World.AddChange(new MapChange(this, l, tileData));
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
		}

		public void ScanWaterTiles()
		{
			foreach (var p in this.Size.Range())
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


		bool CanWaterFlow(IntPoint3 from, IntPoint3 to)
		{
			if (!this.Contains(to))
				return false;

			IntVector3 v = to - from;

			Debug.Assert(v.IsNormal);

			var dstTerrain = GetTerrain(to);
			var dstInter = GetInterior(to);

			if (dstTerrain.IsBlocker || dstInter.IsBlocker)
				return false;

			if (v.Z == 0)
				return true;

			Direction dir = v.ToDirection();

			if (dir == Direction.Up)
				return dstTerrain.IsPermeable == true;

			var srcTerrain = GetTerrain(from);

			if (dir == Direction.Down)
				return srcTerrain.IsPermeable == true;

			throw new Exception();
		}

		void HandleWaterAt(IntPoint3 p, Dictionary<IntPoint3, int> waterChangeMap)
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

			var dirs = DirectionExtensions.CardinalUpDownDirections.ToArray();
			ShuffleArray(dirs);
			bool curLevelChanged = false;

			for (int i = 0; i < dirs.Length; ++i)
			{
				var d = dirs[i];
				var pp = p + d;

				if (!CanWaterFlow(p, pp))
					continue;

				int neighLevel;
				if (!waterChangeMap.TryGetValue(pp, out neighLevel))
					neighLevel = m_tileGrid.GetWaterLevel(pp);

				int flow;
				if (d == Direction.Up)
				{
					if (curLevel > TileData.MaxWaterLevel)
					{
						flow = curLevel - (neighLevel + TileData.MaxCompress) - 1;
						flow = MyMath.Clamp(flow, curLevel - TileData.MaxWaterLevel, 0);
					}
					else
						flow = 0;

				}
				else if (d == Direction.Down)
				{
					if (neighLevel < TileData.MaxWaterLevel)
						flow = TileData.MaxWaterLevel - neighLevel;
					else if (curLevel >= TileData.MaxWaterLevel)
						flow = curLevel - neighLevel + TileData.MaxCompress;
					else
						flow = 0;

					flow = MyMath.Clamp(flow, curLevel, 0);
				}
				else
				{
					if (curLevel > TileData.MinWaterLevel && curLevel > neighLevel)
					{
						int diff = curLevel - neighLevel;
						flow = (diff + 5) / 6;
						Debug.Assert(flow < curLevel);
						//flow = Math.Min(flow, curLevel - 1);
						//flow = IntClamp(flow, curLevel > 1 ? curLevel - 1 : 0, neighLevel > 1 ? -neighLevel + 1 : 0);
					}
					else
					{
						flow = 0;
					}
				}

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
			IntPoint3[] waterTiles = m_waterTiles.ToArray();
			Dictionary<IntPoint3, int> waterChangeMap = new Dictionary<IntPoint3, int>();

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

				MapChanged(p, td);

				if (level > 0)
					m_waterTiles.Add(p);
				else
					m_waterTiles.Remove(p);
			}
		}

		void Tick()
		{
			HandleWater();
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

		public TileData GetTileData(IntPoint3 l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetTileFlags(IntPoint3 l, TileFlags flags)
		{
			return (m_tileGrid.GetFlags(l) & flags) != 0;
		}

		public void SetTerrain(IntPoint3 p, TerrainID terrainID, MaterialID materialID)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			var oldData = GetTileData(p);

			m_tileGrid.SetTerrain(p, terrainID, materialID);

			var data = m_tileGrid.GetTileData(p);

			MapChanged(p, data);

			if (this.TerrainOrInteriorChanged != null)
				this.TerrainOrInteriorChanged(p, oldData, data);
		}

		public void SetInterior(IntPoint3 p, InteriorID interiorID, MaterialID materialID)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			var oldData = GetTileData(p);

			m_tileGrid.SetInterior(p, interiorID, materialID);

			var data = m_tileGrid.GetTileData(p);

			MapChanged(p, data);

			if (this.TerrainOrInteriorChanged != null)
				this.TerrainOrInteriorChanged(p, oldData, data);
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			var oldData = GetTileData(p);

			m_tileGrid.SetTileData(p, data);

			MapChanged(p, data);

			if (this.TerrainOrInteriorChanged != null)
				this.TerrainOrInteriorChanged(p, oldData, data);

			if (data.WaterLevel > 0)
				m_waterTiles.Add(p);
			else
				m_waterTiles.Remove(p);
		}

		public void SetWaterLevel(IntPoint3 l, byte waterLevel)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			m_tileGrid.SetWaterLevel(l, waterLevel);

			var data = m_tileGrid.GetTileData(l);

			MapChanged(l, data);

			if (waterLevel > 0)
				m_waterTiles.Add(l);
			else
				m_waterTiles.Remove(l);
		}

		public void SetTileFlags(IntPoint3 l, TileFlags flags, bool value)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(this.World.IsWritable);

			this.Version += 1;

			if (value)
				m_tileGrid.SetFlags(l, flags);
			else
				m_tileGrid.ClearFlags(l, flags);

			var d = m_tileGrid.GetTileData(l);

			MapChanged(l, d);
		}

		public IEnumerable<IMovableObject> GetContents(IntRectZ rect)
		{
			var obs = m_contentArray[rect.Z];

			return obs.Where(o => rect.Contains(o.Location));
		}

		IEnumerable<IMovableObject> IEnvironmentObject.GetContents(IntPoint3 l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}

		public IEnumerable<MovableObject> GetContents(IntPoint3 l)
		{
			var list = m_contentArray[l.Z];
			return list.Where(o => o.Location == l);
		}


		public override bool OkToAddChild(MovableObject ob, IntPoint3 p)
		{
			Debug.Assert(this.World.IsWritable);

			if (!this.Contains(p))
				return false;

			if (!EnvironmentHelpers.CanEnter(this, p))
				return false;

			return true;
		}

		protected override void OnChildAdded(MovableObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(!list.Contains(child));
			list.Add(child);
		}

		protected override void OnChildRemoved(MovableObject child)
		{
			var list = m_contentArray[child.Z];
			Debug.Assert(list.Contains(child));
			list.Remove(child);
		}


		public override bool OkToMoveChild(MovableObject ob, Direction dir, IntPoint3 dstLoc)
		{
			return EnvironmentHelpers.CanMoveFromTo(this, ob.Location, dir);
		}

		protected override void OnChildMoved(MovableObject child, IntPoint3 srcLoc, IntPoint3 dstLoc)
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


		public IEnumerable<Direction> GetDirectionsFrom(IntPoint3 p)
		{
			return EnvironmentHelpers.GetDirectionsFrom(this, p);
		}

		public void AddLargeObject(AreaObject ob)
		{
			Debug.Assert(this.World.IsWritable);

			Debug.Assert(m_largeObjectSet.Any(b => b.Area.IntersectsWith(ob.Area)) == false);
			Debug.Assert(ob.IsInitialized == false);

			m_largeObjectSet.Add(ob);

			if (this.LargeObjectAdded != null)
				LargeObjectAdded(ob);
		}

		public void RemoveLargeObject(AreaObject ob)
		{
			Debug.Assert(this.World.IsWritable);
			Debug.Assert(m_largeObjectSet.Contains(ob));

			m_largeObjectSet.Remove(ob);

			if (this.LargeObjectRemoved != null)
				LargeObjectRemoved(ob);
		}

		public AreaObject GetLargeObjectAt(IntPoint3 p)
		{
			return m_largeObjectSet.SingleOrDefault(b => b.Contains(p));
		}

		public T GetLargeObjectAt<T>(IntPoint3 p) where T : AreaObject
		{
			return m_largeObjectSet.OfType<T>().SingleOrDefault(b => b.Contains(p));
		}

		public IEnumerable<AreaObject> GetLargeObjects()
		{
			return m_largeObjectSet;
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (MapData)baseData;

			data.VisibilityMode = this.VisibilityMode;
			data.Size = this.Size;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			var data = new MapData();
			CollectObjectData(data, visibility);
			player.Send(new Messages.ObjectDataMessage(data));

			var sw = Stopwatch.StartNew();
			SendMapTiles(player);
			sw.Stop();
			Trace.TraceInformation("Sending MapTiles took {0} ms", sw.ElapsedMilliseconds);

			foreach (var ob in this.Inventory)
			{
				var vis = player.GetObjectVisibility(ob);

				if (vis != ObjectVisibility.None)
					ob.SendTo(player, vis);
			}

			foreach (var o in m_largeObjectSet)
			{
				o.SendTo(player, ObjectVisibility.All);
			}
		}

		void SendMapTiles(IPlayer player)
		{
			var visionTracker = player.GetVisionTracker(this);

			int w = this.Width;
			int h = this.Height;
			int d = this.Depth;

#if !asd
			var queue = new BlockingCollection<Tuple<int, byte[]>>();

			var writerTask = Task.Factory.StartNew(() =>
				{
					foreach (var tuple in queue.GetConsumingEnumerable())
					{
						int z = tuple.Item1;
						var arr = tuple.Item2;

						var msg = new Messages.MapDataTerrainsMessage() { Environment = this.ObjectID };
						msg.Bounds = new IntCuboid(0, 0, z, w, h, 1);

						msg.TerrainData = arr;
						player.Send(msg);
						//Trace.TraceError("Sent {0}", z);
					}
				});


			Parallel.For(0, d, z =>
			{
				using (var memStream = new MemoryStream())
				{
					using (var compStream = new System.IO.Compression.DeflateStream(memStream, CompressionMode.Compress))
					using (var bufferStream = new BufferedStream(compStream))
					using (var writer = new BinaryWriter(bufferStream))
					{
						for (int y = 0; y < h; ++y)
						{
							for (int x = 0; x < w; ++x)
							{
								var p = new IntPoint3(x, y, z);

								ulong v;

								if (!visionTracker.Sees(p))
									v = 0;
								else
									v = m_tileGrid.GetTileData(p).Raw;

								writer.Write(v);
							}
						}
					}

					queue.Add(new Tuple<int, byte[]>(z, memStream.ToArray()));
				}
			});

			queue.CompleteAdding();

			writerTask.Wait();
#endif
#if asd


			for (int i = 0; i < totalMsgs; ++i)
			{
				int z = i * planesPerMsg;

				using (var memStream = new MemoryStream())
				{
					using (var compressStream = new DeflateStream(memStream, CompressionMode.Compress, true))
					using (var bufferedStream = new BufferedStream(compressStream))
					using (var streamWriter = new BinaryWriter(bufferedStream))
					{
						var range = IntPoint3.Range(0, 0, z, size.Width, size.Height, size.Depth);

						foreach (var p in range)
						{
							ulong v;

							if (!visionTracker.Sees(p))
								v = 0;
							else
								v = m_tileGrid.GetTileData(p).Raw;

							streamWriter.Write(v);
						}
					}

					var msg = new Messages.MapDataTerrainsMessage()
					{
						Environment = this.ObjectID,
						Bounds = new IntCuboid(0, 0, z, size.Width, size.Height, size.Depth),
						TerrainData = memStream.ToArray(),
					};
					player.Send(msg);
					Trace.TraceError("Sent {0}", z);
				}
			}
#endif
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
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

		sealed class LargeObjectSetConv : Dwarrowdelf.ISaveGameConverter
		{
			public object ConvertToSerializable(object value)
			{
				var set = (HashSet<AreaObject>)value;
				return set.ToArray();
			}

			public object ConvertFromSerializable(object value)
			{
				var arr = (AreaObject[])value;
				return new HashSet<AreaObject>(arr);
			}

			public Type InputType { get { return typeof(HashSet<AreaObject>); } }

			public Type OutputType { get { return typeof(AreaObject[]); } }
		}

		sealed class TileGridReaderWriter : ISaveGameReaderWriter
		{
			public void Write(Newtonsoft.Json.JsonWriter writer, object value)
			{
				var grid = (TileGrid)value;

				int w = grid.Grid.GetLength(2);
				int h = grid.Grid.GetLength(1);
				int d = grid.Grid.GetLength(0);

				writer.WriteStartObject();

				writer.WritePropertyName("Width");
				writer.WriteValue(w);
				writer.WritePropertyName("Height");
				writer.WriteValue(h);
				writer.WritePropertyName("Depth");
				writer.WriteValue(d);

				writer.WritePropertyName("TileData");
				writer.WriteStartArray();

				var queue = new BlockingCollection<Tuple<int, byte[]>>();

				var writerTask = Task.Factory.StartNew(() =>
				{
					foreach (var tuple in queue.GetConsumingEnumerable())
					{
						writer.WriteValue(tuple.Item1);
						writer.WriteValue(tuple.Item2);
					}
				});

				Parallel.For(0, d, z =>
				{
					using (var memStream = new MemoryStream())
					{
						using (var compressStream = new DeflateStream(memStream, CompressionMode.Compress, true))
						using (var bufferedStream = new BufferedStream(compressStream))
						using (var streamWriter = new BinaryWriter(bufferedStream))
						{
							var srcArr = grid.Grid;

							for (int y = 0; y < h; ++y)
								for (int x = 0; x < w; ++x)
									streamWriter.Write(srcArr[z, y, x].Raw);
						}

						queue.Add(new Tuple<int, byte[]>(z, memStream.ToArray()));
					}
				});

				queue.CompleteAdding();

				writerTask.Wait();

				writer.WriteEndArray();
				writer.WriteEndObject();
			}

			static void ReadAndValidate(Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.JsonToken token)
			{
				reader.Read();
				if (reader.TokenType != token)
					throw new Exception();
			}

			static int ReadIntProperty(Newtonsoft.Json.JsonReader reader, string propertyName)
			{
				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != propertyName)
					throw new Exception();

				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.Integer)
					throw new Exception();

				return (int)(long)reader.Value;
			}

			public object Read(Newtonsoft.Json.JsonReader reader)
			{
				if (reader.TokenType != Newtonsoft.Json.JsonToken.StartObject)
					throw new Exception();

				int w = ReadIntProperty(reader, "Width");
				int h = ReadIntProperty(reader, "Height");
				int d = ReadIntProperty(reader, "Depth");

				var grid = new TileGrid(new IntSize3(w, h, d));
				var dstArr = grid.Grid;

				reader.Read();
				if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != "TileData")
					throw new Exception();

				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.StartArray);

				var queue = new BlockingCollection<Tuple<int, byte[]>>();

				var readerTask = Task.Factory.StartNew(() =>
				{
					for (int i = 0; i < d; ++i)
					{
						reader.Read();
						int z = (int)(long)reader.Value;

						byte[] buf = reader.ReadAsBytes();

						queue.Add(new Tuple<int, byte[]>(z, buf));
					}

					queue.CompleteAdding();
				});

				Parallel.For(0, d, i =>
				{
					var tuple = queue.Take();

					int z = tuple.Item1;
					byte[] arr = tuple.Item2;

					using (var memStream = new MemoryStream(arr))
					{
						using (var decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
						using (var streamReader = new BinaryReader(decompressStream))
						{
							for (int y = 0; y < h; ++y)
								for (int x = 0; x < w; ++x)
									dstArr[z, y, x].Raw = streamReader.ReadUInt64();
						}
					}
				});

				readerTask.Wait();

				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndArray);
				ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndObject);

				return grid;
			}
		}
	}

	sealed class TileGrid
	{
		TileData[, ,] m_grid;
		public TileData[, ,] Grid { get { return m_grid; } }
		public IntSize3 Size { get; private set; }

		TileGrid()
		{
		}

		public TileGrid(IntSize3 size)
		{
			this.Size = size;
			m_grid = new TileData[size.Depth, size.Height, size.Width];
		}

		public TileData GetTileData(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X];
		}

		public TerrainID GetTerrainID(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].TerrainID;
		}

		public MaterialID GetTerrainMaterialID(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].TerrainMaterialID;
		}

		public InteriorID GetInteriorID(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].InteriorID;
		}

		public MaterialID GetInteriorMaterialID(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].InteriorMaterialID;
		}

		public byte GetWaterLevel(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].WaterLevel;
		}

		public TileFlags GetFlags(IntPoint3 p)
		{
			return m_grid[p.Z, p.Y, p.X].Flags;
		}


		public void SetTileData(IntPoint3 p, TileData data)
		{
			m_grid[p.Z, p.Y, p.X] = data;
		}

		public void SetTerrain(IntPoint3 p, TerrainID id, MaterialID matID)
		{
			m_grid[p.Z, p.Y, p.X].TerrainID = id;
			m_grid[p.Z, p.Y, p.X].TerrainMaterialID = matID;
		}

		public void SetTerrainID(IntPoint3 p, TerrainID id)
		{
			m_grid[p.Z, p.Y, p.X].TerrainID = id;
		}

		public void SetTerrainMaterialID(IntPoint3 p, MaterialID id)
		{
			m_grid[p.Z, p.Y, p.X].TerrainMaterialID = id;
		}

		public void SetInterior(IntPoint3 p, InteriorID id, MaterialID matID)
		{
			m_grid[p.Z, p.Y, p.X].InteriorID = id;
			m_grid[p.Z, p.Y, p.X].InteriorMaterialID = matID;
		}

		public void SetInteriorID(IntPoint3 p, InteriorID id)
		{
			m_grid[p.Z, p.Y, p.X].InteriorID = id;
		}

		public void SetInteriorMaterialID(IntPoint3 p, MaterialID id)
		{
			m_grid[p.Z, p.Y, p.X].InteriorMaterialID = id;
		}

		public void SetWaterLevel(IntPoint3 p, byte waterLevel)
		{
			m_grid[p.Z, p.Y, p.X].WaterLevel = waterLevel;
		}

		public void SetFlags(IntPoint3 p, TileFlags flags)
		{
			m_grid[p.Z, p.Y, p.X].Flags |= flags;
		}

		public void ClearFlags(IntPoint3 p, TileFlags flags)
		{
			m_grid[p.Z, p.Y, p.X].Flags &= ~flags;
		}
	}

	public sealed class EnvironmentObjectBuilder
	{
		TileGrid m_tileGrid;
		IntSize3 m_size;

		public IntCuboid Bounds { get { return new IntCuboid(m_size); } }
		public int Width { get { return m_size.Width; } }
		public int Height { get { return m_size.Height; } }
		public int Depth { get { return m_size.Depth; } }

		public VisibilityMode VisibilityMode { get; set; }

		internal TileGrid Grid { get { return m_tileGrid; } }

		public EnvironmentObjectBuilder(IntSize3 size, VisibilityMode visibilityMode)
		{
			m_size = size;
			m_tileGrid = new TileGrid(size);
			this.VisibilityMode = visibilityMode;
		}

		public EnvironmentObject Create(World world)
		{
			return EnvironmentObject.Create(world, this);
		}

		public bool Contains(IntPoint3 p)
		{
			return p.X >= 0 && p.Y >= 0 && p.Z >= 0 && p.X < this.Width && p.Y < this.Height && p.Z < this.Depth;
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

		public TileData GetTileData(IntPoint3 l)
		{
			return m_tileGrid.GetTileData(l);
		}

		public byte GetWaterLevel(IntPoint3 l)
		{
			return m_tileGrid.GetWaterLevel(l);
		}

		public bool GetTileFlag(IntPoint3 l, TileFlags flag)
		{
			return (m_tileGrid.GetFlags(l) & flag) != 0;
		}

		public void SetTerrain(IntPoint3 p, TerrainID terrainID, MaterialID materialID)
		{
			m_tileGrid.SetTerrain(p, terrainID, materialID);
		}

		public void SetInterior(IntPoint3 p, InteriorID interiorID, MaterialID materialID)
		{
			m_tileGrid.SetInterior(p, interiorID, materialID);
		}

		public void SetTileData(IntPoint3 p, TileData data)
		{
			m_tileGrid.SetTileData(p, data);
		}

		public void SetTileFlags(IntPoint3 l, TileFlags flags, bool value)
		{
			if (value)
				m_tileGrid.SetFlags(l, flags);
			else
				m_tileGrid.ClearFlags(l, flags);
		}
	}
}
