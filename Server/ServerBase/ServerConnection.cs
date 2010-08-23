using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using MyGame.Messages;
using System.IO;

namespace MyGame.Server
{
	public class ServerConnection
	{
		class WorldInvokeAttribute : Attribute
		{
			public WorldInvokeStyle Style { get; set; }

			public WorldInvokeAttribute(WorldInvokeStyle style)
			{
				this.Style = style;
			}
		}

		enum WorldInvokeStyle
		{
			/// <summary>
			/// Call directly
			/// </summary>
			None,
			/// <summary>
			/// Use world.BeginInvoke()
			/// </summary>
			Normal,
			/// <summary>
			/// Use world.BeginInvokeInstant()
			/// </summary>
			Instant,
		}

		class InvokeInfo
		{
			public Action<Message> Action;
			public WorldInvokeStyle Style;
		}


		static int s_userIDs = 1;
		Dictionary<Type, InvokeInfo> m_handlerMap = new Dictionary<Type, InvokeInfo>();
		World m_world;
		bool m_charLoggedIn;

		int m_userID;

		// this user sees all
		bool m_seeAll = true;

		List<Living> m_controllables = new List<Living>();

		Connection m_connection;
		bool m_userLoggedIn;

		Microsoft.Scripting.Hosting.ScriptEngine m_scriptEngine;
		Microsoft.Scripting.Hosting.ScriptScope m_scriptScope;

		MyStream m_scriptOutputStream;

		class MyStream : Stream
		{
			Action<Messages.Message> m_sender;
			MemoryStream m_stream = new MemoryStream();

			public MyStream(Action<Messages.Message> sender)
			{
				m_sender = sender;
			}

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }

			public override void Flush()
			{
				if (m_stream.Position == 0)
					return;

				var text = System.Text.Encoding.Unicode.GetString(m_stream.GetBuffer(), 0, (int)m_stream.Position);
				m_stream.Position = 0;
				m_stream.SetLength(0);
				var msg = new Messages.IPOutputMessage() { Text = text };
				m_sender(msg);
			}

			public override long Length { get { throw new NotImplementedException(); } }

			public override long Position
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (count == 0)
					return;

				m_stream.Write(buffer, offset, count);
			}
		}


		public ServerConnection(Connection conn, World world)
		{
			m_world = world;

			m_connection = conn;
			m_connection.ReceiveEvent += OnReceiveMessage;
			m_connection.DisconnectEvent += OnDisconnect;

			m_scriptOutputStream = new MyStream(Send);

			m_scriptEngine = IronPython.Hosting.Python.CreateEngine();
			m_scriptEngine.Runtime.IO.SetOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);
			m_scriptEngine.Runtime.IO.SetErrorOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);

			m_scriptScope = m_scriptEngine.CreateScope();
			m_scriptScope.SetVariable("world", m_world);
			m_scriptScope.SetVariable("get", new Func<object, IIdentifiable>(m_world.IPGet));

			m_scriptEngine.Execute("import clr", m_scriptScope);
			m_scriptEngine.Execute("clr.AddReference('MyGame.Common')", m_scriptScope);
			m_scriptEngine.Execute("import MyGame", m_scriptScope);
		}

		protected void OnDisconnect()
		{
			m_world.BeginInvokeInstant(new Action(ClientDisconnected), null);
		}

		void ClientDisconnected()
		{
			MyDebug.WriteLine("Client Disconnect");

			if (m_userLoggedIn)
			{
				m_world.RemoveUser(this);
				m_world.HandleEndOfTurn -= HandleEndOfTurn;
			}

			m_world = null;

			m_connection.ReceiveEvent -= OnReceiveMessage;
			m_connection.DisconnectEvent -= OnDisconnect;
			m_connection = null;
		}

		void Send(Message msg)
		{
			m_connection.Send(msg);
		}

		void Send(IEnumerable<Message> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}

		void OnReceiveMessage(Message msg)
		{
			InvokeInfo f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				System.Reflection.MethodInfo mi;
				f = new InvokeInfo();
				f.Action = WrapperGenerator.CreateHandlerWrapper<Message>("ReceiveMessage", t, this, out mi);

				if (f == null)
					throw new Exception("Unknown Message");

				var attr = (WorldInvokeAttribute)Attribute.GetCustomAttribute(mi, typeof(WorldInvokeAttribute));

				if (attr == null || attr.Style == WorldInvokeStyle.None)
					f.Style = WorldInvokeStyle.None;
				else if (attr.Style == WorldInvokeStyle.Normal)
					f.Style = WorldInvokeStyle.Normal;
				else if (attr.Style == WorldInvokeStyle.Instant)
					f.Style = WorldInvokeStyle.Instant;
				else
					throw new Exception();

				m_handlerMap[t] = f;
			}

			switch (f.Style)
			{
				case WorldInvokeStyle.None:
					f.Action(msg);
					break;
				case WorldInvokeStyle.Normal:
					m_world.BeginInvoke(f.Action, msg);
					break;
				case WorldInvokeStyle.Instant:
					m_world.BeginInvokeInstant(f.Action, msg);
					break;
				default:
					throw new Exception();
			}
		}



		[WorldInvoke(WorldInvokeStyle.Normal)]
		void ReceiveMessage(LogOnRequestMessage msg)
		{
			string name = msg.Name;

			MyDebug.WriteLine("LogOn {0}", name);

			m_userID = s_userIDs++;

			Send(new Messages.LogOnReplyMessage() { UserID = m_userID, IsSeeAll = m_seeAll });

			if (m_seeAll)
			{
				foreach (var env in m_world.Environments)
					env.SerializeTo(Send);
			}

			m_world.HandleEndOfTurn += HandleEndOfTurn;
			m_world.AddUser(this);

			m_userLoggedIn = true;
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(LogOffRequestMessage msg)
		{
			MyDebug.WriteLine("Logout");

			if (m_charLoggedIn)
				ReceiveMessage(new LogOffCharRequestMessage()); // XXX

			m_scriptScope.RemoveVariable("world");

			m_world.RemoveUser(this);
			m_world.HandleEndOfTurn -= HandleEndOfTurn;

			m_userLoggedIn = false;

			Send(new Messages.LogOffReplyMessage());
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(SetTilesMessage msg)
		{
			ObjectID mapID = msg.MapID;
			IntCuboid r = msg.Cube;

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			foreach (var p in r.Range())
			{
				if (!env.Bounds.Contains(p))
					continue;

				var tileData = env.GetTileData(p);

				if (msg.InteriorID.HasValue)
					tileData.InteriorID = msg.InteriorID.Value;
				if (msg.InteriorMaterialID.HasValue)
					tileData.InteriorMaterialID = msg.InteriorMaterialID.Value;

				if (msg.FloorID.HasValue)
					tileData.FloorID = msg.FloorID.Value;
				if (msg.FloorMaterialID.HasValue)
					tileData.FloorMaterialID = msg.FloorMaterialID.Value;

				if (msg.WaterLevel.HasValue)
					tileData.WaterLevel = msg.WaterLevel.Value;
				if (msg.Grass.HasValue)
					tileData.Grass = msg.Grass.Value;

				env.SetTileData(p, tileData);
			}

			env.ScanWaterTiles();
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(CreateBuildingMessage msg)
		{
			ObjectID mapID = msg.MapID;
			var r = msg.Area;
			var z = msg.Z;
			var id = msg.ID;

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			var building = new BuildingObject(m_world, id) { Area = r, Z = z };
			foreach (var p2d in building.Area.Range())
			{
				var p = new IntPoint3D(p2d, building.Z);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
			}
			env.AddBuilding(building);
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(ProceedTickMessage msg)
		{
			MyDebug.WriteLine("ProceedTick command");
			m_world.RequestTick();
		}

		Random m_random = new Random();
		IntPoint3D GetRandomSurfaceLocation(Environment env, int zLevel)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), zLevel);
			} while (!env.CanEnter(p));

			return p;
		}

		/* functions for livings */
		[WorldInvoke(WorldInvokeStyle.Normal)]
		void ReceiveMessage(LogOnCharRequestMessage msg)
		{
			string name = msg.Name;

			MyDebug.WriteLine("LogOnChar {0}", name);

			var env = m_world.Environments.First(); // XXX entry location

#if asd
			var player = new Living(m_world, "player")
			{
				SymbolID = SymbolID.Player,
				Actor = new InteractiveActor(),
			};

			MyDebug.WriteLine("Player ob id {0}", player.ObjectID);

			m_controllables.Add(player);

			var diamond = Materials.Diamond.ID;

			ItemObject item = new ItemObject(m_world)
			{
				Name = "jalokivi1",
				SymbolID = SymbolID.Gem,
				MaterialID = diamond,
			};
			item.MoveTo(player);

			item = new ItemObject(m_world)
			{
				Name = "jalokivi2",
				SymbolID = SymbolID.Gem,
				Color = GameColor.Green,
				MaterialID = diamond,
			};
			item.MoveTo(player);

			var pp = GetRandomSurfaceLocation(env, 9);
			if (!player.MoveTo(env, pp))
				throw new Exception("Unable to move player");

			var inv = player.SerializeInventory();
			Send(inv);

			m_scriptScope.SetVariable("me", player);
#if qwe
			var pet = new Living(m_world);
			pet.SymbolID = SymbolID.Monster;
			pet.Name = "lemmikki";
			pet.Actor = new InteractiveActor();
			m_controllables.Add(pet);

			pet.MoveTo(player.Environment, player.Location + new IntVector(1, 0));
#endif

#else
			var rand = new Random();
			for (int i = 0; i < 5; ++i)
			{
				IntPoint3D p;
				do
				{
					p = new IntPoint3D(rand.Next(env.Width), rand.Next(env.Height), 9);
				} while (env.GetInteriorID(p) != InteriorID.Empty);

				var player = new Living(m_world, String.Format("Dwarf{0}", i))
				{
					SymbolID = SymbolID.Player,
					Color = (GameColor)rand.Next((int)GameColor.NumColors),
				};
				player.Actor = new InteractiveActor(player);

				m_controllables.Add(player);
				if (!player.MoveTo(env, p))
					throw new Exception();
			}
#endif

			m_charLoggedIn = true;
			Send(new Messages.LogOnCharReplyMessage());
			Send(new Messages.ControllablesDataMessage() { Controllables = m_controllables.Select(l => l.ObjectID).ToArray() });
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(LogOffCharRequestMessage msg)
		{
			MyDebug.WriteLine("LogOffChar");

			m_scriptScope.RemoveVariable("me");

			foreach (var l in m_controllables)
			{
				l.Cleanup();
			}

			m_controllables.Clear();

			Send(new Messages.ControllablesDataMessage() { Controllables = new ObjectID[0] });
			Send(new Messages.LogOffCharReplyMessage());
			m_charLoggedIn = false;
		}

		void ReceiveMessage(EnqueueActionMessage msg)
		{
			var action = msg.Action;

			try
			{
				if (action.TransactionID == 0)
					throw new Exception();

				var living = m_controllables.SingleOrDefault(l => l.ObjectID == action.ActorObjectID);

				if (living == null)
					throw new Exception("Illegal ob id");

				action.UserID = m_userID;

				living.EnqueueAction(action);
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(IPCommandMessage msg)
		{
			MyDebug.WriteLine("IronPythonCommand");

			var script = msg.Text;

			try
			{
				var r = m_scriptEngine.ExecuteAndWrap(script, m_scriptScope);
				m_scriptScope.SetVariable("ret", r);
				m_scriptEngine.Execute("print ret", m_scriptScope);
			}
			catch (Exception e)
			{
				var str = "IP error:\n" + e.Message + "\n";
				Send(new IPOutputMessage() { Text = str });
			}
		}


		// These are used to determine new tiles and objects in sight
		HashSet<Environment> m_knownEnvironments = new HashSet<Environment>();

		Dictionary<Environment, HashSet<IntPoint3D>> m_oldKnownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_oldKnownObjects = new HashSet<ServerGameObject>();

		Dictionary<Environment, HashSet<IntPoint3D>> m_newKnownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_newKnownObjects = new HashSet<ServerGameObject>();

		// Called from the world at the end of turn
		void HandleEndOfTurn(IEnumerable<Change> changes, IEnumerable<Event> events)
		{
			// if the user sees all, no need to send new terrains/objects
			if (!m_seeAll)
				HandleNewTerrainsAndObjects(m_controllables);

			HandleChanges(changes);
			HandleEvents(events);
		}

		void HandleNewTerrainsAndObjects(IList<Living> friendlies)
		{
			m_oldKnownLocations = m_newKnownLocations;
			m_newKnownLocations = CollectLocations(friendlies);

			m_oldKnownObjects = m_newKnownObjects;
			m_newKnownObjects = CollectObjects(m_newKnownLocations);

			var revealedLocations = CollectRevealedLocations(m_oldKnownLocations, m_newKnownLocations);
			var revealedObjects = CollectRevealedObjects(m_oldKnownObjects, m_newKnownObjects);
			var revealedEnvironments = m_newKnownLocations.Keys.Except(m_knownEnvironments).ToArray();

			m_knownEnvironments.UnionWith(revealedEnvironments);

			SendNewEnvironments(revealedEnvironments);
			SendNewTerrains(revealedLocations);
			SendNewObjects(revealedObjects);
		}

		// Collect all environments and locations that friendlies see
		Dictionary<Environment, HashSet<IntPoint3D>> CollectLocations(IEnumerable<Living> friendlies)
		{
			var knownLocs = new Dictionary<Environment, HashSet<IntPoint3D>>();

			foreach (var l in friendlies)
			{
				if (l.Environment == null)
					continue;

				IEnumerable<IntPoint3D> locList;

				/* for AllVisible maps we don't track visible locations, but we still
				 * need to handle newly visible maps */
				if (l.Environment.VisibilityMode == VisibilityMode.AllVisible)
					locList = new List<IntPoint3D>();
				else
					locList = l.GetVisibleLocations().Select(p => new IntPoint3D(p.X, p.Y, l.Z));

				if (!knownLocs.ContainsKey(l.Environment))
					knownLocs[l.Environment] = new HashSet<IntPoint3D>();

				knownLocs[l.Environment].UnionWith(locList);
			}

			return knownLocs;
		}

		// Collect all objects in the given location map
		static HashSet<ServerGameObject> CollectObjects(Dictionary<Environment, HashSet<IntPoint3D>> knownLocs)
		{
			var knownObs = new HashSet<ServerGameObject>();

			foreach (var kvp in knownLocs)
			{
				var env = kvp.Key;
				var newLocs = kvp.Value;

				foreach (var p in newLocs)
				{
					var obList = env.GetContents(p);
					if (obList != null)
						knownObs.UnionWith(obList);
				}
			}

			return knownObs;
		}

		// Collect locations that are newly visible
		static Dictionary<Environment, HashSet<IntPoint3D>> CollectRevealedLocations(Dictionary<Environment, HashSet<IntPoint3D>> oldLocs,
			Dictionary<Environment, HashSet<IntPoint3D>> newLocs)
		{
			var revealedLocs = new Dictionary<Environment, HashSet<IntPoint3D>>();

			foreach (var kvp in newLocs)
			{
				if (oldLocs.ContainsKey(kvp.Key))
					revealedLocs[kvp.Key] = new HashSet<IntPoint3D>(kvp.Value.Except(oldLocs[kvp.Key]));
				else
					revealedLocs[kvp.Key] = kvp.Value;
			}

			return revealedLocs;
		}

		// Collect objects that are newly visible
		static ServerGameObject[] CollectRevealedObjects(HashSet<ServerGameObject> oldObjects, HashSet<ServerGameObject> newObjects)
		{
			var revealedObs = newObjects.Except(oldObjects).ToArray();
			return revealedObs;
		}

		private void SendNewEnvironments(IEnumerable<Environment> revealedEnvironments)
		{
			// send full data for AllVisible envs, and intro for other maps
			foreach (var env in revealedEnvironments)
			{
				if (env.VisibilityMode == VisibilityMode.AllVisible)
				{
					env.SerializeTo(Send);
				}
				else
				{
					var msg = new Messages.MapDataMessage()
					{
						Environment = env.ObjectID,
						VisibilityMode = env.VisibilityMode,
					};
					Send(msg);
				}
			}
		}

		void SendNewTerrains(Dictionary<Environment, HashSet<IntPoint3D>> revealedLocations)
		{
			var msgs = revealedLocations.Where(kvp => kvp.Value.Count() > 0).
				Select(kvp => (Messages.Message)new Messages.MapDataTerrainsListMessage()
				{
					Environment = kvp.Key.ObjectID,
					TileDataList = kvp.Value.Select(l =>
						new Tuple<IntPoint3D, TileData>(l, kvp.Key.GetTileData(l))
						).ToArray(),
				});


			Send(msgs);
		}

		void SendNewObjects(IEnumerable<ServerGameObject> revealedObjects)
		{
			var msgs = revealedObjects.Select(o => o.Serialize());
			Send(msgs);
		}



		void HandleEvents(IEnumerable<Event> events)
		{
			events = events.Where(EventFilter);
			var msgs = events.Select(e => new Messages.EventMessage(e));
			Send(msgs);
		}

		bool EventFilter(Event @event)
		{
			if (@event is ActionProgressEvent)
			{
				var e = (ActionProgressEvent)@event;
				return e.UserID == m_userID;
			}

			if (@event is ActionRequiredEvent)
			{
				var e = (ActionRequiredEvent)@event;
				return m_controllables.Any(l => l.ObjectID == e.ObjectID);
			}

			if (@event is TickChangeEvent)
				return true;

			return true;
		}



		void HandleChanges(IEnumerable<Change> changes)
		{
			var changeMsgs = CollectChanges(m_controllables, changes);
			Send(changeMsgs);
		}

		IEnumerable<Messages.Message> CollectChanges(IEnumerable<Living> friendlies, IEnumerable<Change> changes)
		{
			IEnumerable<Messages.Message> msgs = new List<Messages.Message>();

			if (!m_seeAll)
			{
				// We don't collect newly visible terrains/objects on AllVisible maps.
				// However, we still need to tell about newly created objects that come
				// to AllVisible maps.
				var newObjects = changes.OfType<ObjectMoveChange>().
					Where(c => c.Source != c.Destination &&
						c.Destination is Environment &&
						((Environment)c.Destination).VisibilityMode == VisibilityMode.AllVisible).
					Select(c => (ServerGameObject)c.Object);

				var newObMsgs = ObjectsToMessages(newObjects);
				msgs = msgs.Concat(newObMsgs);

				// filter changes that friendlies see
				changes = changes.Where(c => friendlies.Any(l => ChangeFilter(l, c)));
			}

			var changeMsgs = changes.Select(ChangeToMessage);

			// NOTE: send changes last, so that object/map/tile information has already
			// been received by the client
			msgs = msgs.Concat(changeMsgs);

			return msgs;
		}

		// can living see the change?
		bool ChangeFilter(Living living, Change change)
		{
			// XXX these checks are not totally correct. objects may have changed after
			// the creation of the change, for example moved. Should changes contain
			// all the information needed for these checks?
			if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;

				if (living == c.Object)
					return true;

				if (living.Sees(c.Source, c.SourceLocation))
					return true;

				if (living.Sees(c.Destination, c.DestinationLocation))
					return true;

				return false;
			}
			else if (change is MapChange)
			{
				var c = (MapChange)change;

				return living.Sees(c.Map, c.Location);
			}
			else if (change is ObjectDestructedChange)
			{
				return true;
			}
			else if (change is ObjectCreatedChange)
			{
				return true;
			}
			else if (change is FullObjectChange)
			{
				var c = (FullObjectChange)change;
				return c.Object == living;
			}
			else if (change is PropertyChange)
			{
				var c = (PropertyChange)change;

				if (c.Object == living)
					return true;

				if (c.Property.Visibility == PropertyVisibility.Friendly)
				{
					// xxx should check if the object is friendly
					// return false for now, as all friendlies are controllables, thus we will still see it
					// because the check above will return true to that controllable
					return false;
				}
				else if (c.Property.Visibility == PropertyVisibility.Public)
				{
					ServerGameObject ob = (ServerGameObject)c.Object;

					return living.Sees(ob.Environment, ob.Location);
				}
			}

			throw new Exception();
		}


		public Messages.Message ChangeToMessage(Change change)
		{
			if (change is ObjectMoveChange)
			{
				ObjectMoveChange mc = (ObjectMoveChange)change;
				return new Messages.ObjectMoveMessage(mc.Object, mc.SourceMapID, mc.SourceLocation,
					mc.DestinationMapID, mc.DestinationLocation);
			}
			else if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				return new Messages.MapDataTerrainsListMessage()
				{
					Environment = mc.MapID,
					TileDataList = new Tuple<IntPoint3D, TileData>[]
					{
						new Tuple<IntPoint3D, TileData>(mc.Location, mc.TileData)
					}
				};
			}
			else if (change is FullObjectChange)
			{
				var c = (FullObjectChange)change;
				return c.ObjectData;
			}
			else if (change is ObjectCreatedChange)
			{
				var c = (ObjectCreatedChange)change;
				var o = (BaseGameObject)c.Object;
				return o.Serialize();
			}
			else if (change is ObjectDestructedChange)
			{
				return new Messages.ObjectDestructedMessage() { ObjectID = ((ObjectDestructedChange)change).ObjectID };
			}
			else if (change is PropertyChange)
			{
				var c = (PropertyChange)change;
				return new Messages.PropertyDataMessage() { ObjectID = c.ObjectID, PropertyID = c.PropertyID, Value = c.Value };
			}

			throw new Exception("Unknown Change type");
		}



		static IEnumerable<Messages.Message> ObjectsToMessages(IEnumerable<BaseGameObject> revealedObs)
		{
			var msgs = revealedObs.Select(o => o.Serialize());
			return msgs;
		}
	}
}
