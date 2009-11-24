using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using MyGame.ClientMsgs;

namespace MyGame
{
	public class ServerConnection : Connection
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
			None,
			Normal,
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
		Living m_player;

		int m_userID;

		// this user sees all
		bool m_seeAll = false;

		// livings used for fov
		List<Living> m_friendlies = new List<Living>();


		public ServerConnection(TcpClient client, World world)
			: base(client)
		{
			m_world = world;
		}

		public void Send(IEnumerable<Message> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}

		protected override void ReceiveMessage(Message msg)
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
		void ReceiveMessage(LogOnRequest msg)
		{
			string name = msg.Name;

			MyDebug.WriteLine("LogOn {0}", name);

			m_userID = s_userIDs++;

			Send(new ClientMsgs.LogOnReply() { UserID = m_userID });

			if (m_seeAll)
			{
				foreach (var env in m_world.Environments)
				{
					var m = env.Serialize();
					Send(m);
				}
			}

			m_world.HandleChangesEvent += HandleChanges;
			m_world.HandleEventsEvent += HandleEvents;
		}

		[WorldInvoke(WorldInvokeStyle.Normal)]
		void ReceiveMessage(LogOffMessage msg)
		{
			MyDebug.WriteLine("Logout");

			if (m_player != null)
				ReceiveMessage(new LogOffCharRequest()); // XXX

			m_world.HandleChangesEvent -= HandleChanges;
			m_world.HandleEventsEvent -= HandleEvents;

			m_world = null;
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(SetTilesMessage msg)
		{
			ObjectID mapID = msg.MapID;
			IntCube r = msg.Cube;
			TileData data = msg.TileData;

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			foreach (var p in r.Range())
			{
				if (!env.Bounds.Contains(p))
					continue;

				env.SetTileData(p, data);
			}
		}

		[WorldInvoke(WorldInvokeStyle.Normal)]
		void ReceiveMessage(ProceedTurnMessage msg)
		{
			MyDebug.WriteLine("ProceedTurn command");
			m_world.RequestTurn();
		}

		/* functions for livings */
		[WorldInvoke(WorldInvokeStyle.Normal)]
		void ReceiveMessage(LogOnCharRequest msg)
		{
			string name = msg.Name;

			MyDebug.WriteLine("LogOnChar {0}", name);

			var env = m_world.Environments.First(); // XXX entry location

			m_world.AddUser(this);

			var obs = m_world.AreaData.Objects;

			m_player = new Living(m_world);
			m_player.SymbolID = obs.Single(o => o.Name == "Player").SymbolID; ;
			m_player.Name = "player";
			m_player.Actor = new InteractiveActor();

			MyDebug.WriteLine("Player ob id {0}", m_player.ObjectID);

			m_friendlies.Add(m_player);
			Send(new ClientMsgs.LogOnCharReply() { PlayerID = m_player.ObjectID });


			ItemObject item = new ItemObject(m_world);
			item.Name = "jalokivi1";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.MoveTo(m_player);

			item = new ItemObject(m_world);
			item.Name = "jalokivi2";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.Color = GameColors.Green;
			item.MoveTo(m_player);

			if (!m_player.MoveTo(env, new IntPoint3D(0, 0, 0)))
				throw new Exception("Unable to move player");

			var inv = m_player.SerializeInventory();
			Send(inv);

			var pet = new Living(m_world);
			pet.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
			pet.Name = "lemmikki";
			pet.Actor = new InteractiveActor();
			m_friendlies.Add(pet);
			Send(new ClientMsgs.LogOnCharReply() { PlayerID = pet.ObjectID });

			pet.MoveTo(m_player.Environment, m_player.Location + new IntVector(1, 0));
		}

		[WorldInvoke(WorldInvokeStyle.Instant)]
		void ReceiveMessage(LogOffCharRequest msg)
		{
			m_player.EnqueueAction(new WaitAction(1));
			m_world.BeginInvoke(new Action(__LogOffChar));
		}

		void __LogOffChar()
		{
			MyDebug.WriteLine("LogOffChar");

			m_friendlies.Remove(m_player);

			m_world.RemoveUser(this);

			m_player.Actor = null;
			m_player.Cleanup();
			m_player = null;

			Send(new ClientMsgs.LogOffCharReply());
		}

		void ReceiveMessage(EnqueueActionMessage msg)
		{
			var action = msg.Action;

			try
			{
				if (action.TransactionID == 0)
					throw new Exception();

				var living = m_friendlies.SingleOrDefault(l => l.ObjectID == action.ActorObjectID);

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




		void HandleEvents(IEnumerable<Event> events)
		{
			events = events.Where(e => EventFilter(e));

			var msgs = events.Select(e => (ClientMsgs.Message)new ClientMsgs.EventMessage(e));

			Send(msgs);
		}

		public bool EventFilter(Event @event)
		{
			if (@event is ActionProgressEvent)
			{
				ActionProgressEvent e = (ActionProgressEvent)@event;
				return e.UserID == m_userID;
			}

			if (@event is TurnChangeEvent)
				return true;

			return true;
		}

		// These are used to determine new tiles and objects in sight
		Dictionary<Environment, HashSet<IntPoint3D>> m_knownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_knownObjects = new HashSet<ServerGameObject>();

		void HandleChanges(IEnumerable<Change> changes)
		{
			IEnumerable<ClientMsgs.Message> msgs = new List<ClientMsgs.Message>();

			// if the user sees all, no need to send new terrains/objects
			if (!m_seeAll)
			{
				var m = CollectNewTerrainsAndObjects(m_friendlies);
				msgs = msgs.Concat(m);
			}

			var changeMsgs = CollectChanges(m_friendlies, changes);
			msgs = msgs.Concat(changeMsgs);

			if (msgs.Count() > 0)
				Send(msgs);
		}

		IEnumerable<ClientMsgs.Message> CollectChanges(IEnumerable<Living> friendlies, IEnumerable<Change> changes)
		{
			IEnumerable<ClientMsgs.Message> msgs = new List<ClientMsgs.Message>();

			if (m_seeAll)
			{
				// If the user sees all, we don't collect newly visible objects. However,
				// we still need to tell about newly created objects.
				var newObjects = changes.
					OfType<ObjectMoveChange>().
					Where(c => c.SourceMapID == ObjectID.NullObjectID).
					Select(c => (ServerGameObject)c.Object);

				var newObMsgs = ObjectsToMessages(newObjects);
				msgs = msgs.Concat(newObMsgs);
			}
			else
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
				changes = changes.Where(c => friendlies.Any(l => l.ChangeFilter(c)));
			}

			var changeMsgs = changes.Select(c => ChangeToMessage(c));

			// NOTE: send changes last, so that object/map/tile information has already
			// been received by the client
			msgs = msgs.Concat(changeMsgs);

			return msgs;
		}

		public ClientMsgs.Message ChangeToMessage(Change change)
		{
			if (change is ObjectMoveChange)
			{
				ObjectMoveChange mc = (ObjectMoveChange)change;
				return new ClientMsgs.ObjectMove(mc.Object, mc.SourceMapID, mc.SourceLocation,
					mc.DestinationMapID, mc.DestinationLocation);
			}
			else if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				return new ClientMsgs.TerrainData()
				{
					Environment = mc.MapID,
					TileDataList = new KeyValuePair<IntPoint3D, TileData>[]
					{
						new KeyValuePair<IntPoint3D, TileData>(mc.Location, mc.TileData)
					}
				};
			}

			throw new Exception();
		}


		IEnumerable<ClientMsgs.Message> CollectNewTerrainsAndObjects(IEnumerable<Living> friendlies)
		{
			List<ClientMsgs.Message> mapMsgs = new List<ClientMsgs.Message>();
			// Collect all locations that friendlies see
			var newKnownLocs = new Dictionary<Environment, HashSet<IntPoint3D>>();
			foreach (Living l in friendlies)
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

				if (!newKnownLocs.ContainsKey(l.Environment))
				{
					newKnownLocs[l.Environment] = new HashSet<IntPoint3D>();

					if (!m_knownLocations.ContainsKey(l.Environment))
					{
						// new environment for this user, so send map info
						var md = new ClientMsgs.MapData()
						{
							Environment = l.Environment.ObjectID,
							VisibilityMode = l.Environment.VisibilityMode,
						};
						mapMsgs.Add(md);

						if (l.Environment.VisibilityMode == VisibilityMode.AllVisible)
						{
							var msg = l.Environment.Serialize();
							mapMsgs.Add(msg);
						}
					}
				}
				newKnownLocs[l.Environment].UnionWith(locList);
			}

			// Collect objects in visible locations
			var newKnownObs = new HashSet<ServerGameObject>();
			foreach (var kvp in newKnownLocs)
			{
				var env = kvp.Key;
				var newLocs = kvp.Value;

				foreach (var p in newLocs)
				{
					var obList = env.GetContents(p);
					if (obList == null)
						continue;
					newKnownObs.UnionWith(obList);
				}
			}

			// Collect locations that are newly visible
			var revealedLocs = new Dictionary<Environment, IEnumerable<IntPoint3D>>();
			foreach (var kvp in newKnownLocs)
			{
				if (m_knownLocations.ContainsKey(kvp.Key))
					revealedLocs[kvp.Key] = kvp.Value.Except(m_knownLocations[kvp.Key]);
				else
					revealedLocs[kvp.Key] = kvp.Value;
			}

			// Collect objects that are newly visible
			var revealedObs = newKnownObs.Except(m_knownObjects);

			m_knownLocations = newKnownLocs;
			m_knownObjects = newKnownObs;

			var terrainMsgs = TilesToMessages(revealedLocs);
			var objectMsgs = ObjectsToMessages(revealedObs);

			return mapMsgs.Concat(terrainMsgs).Concat(objectMsgs);
		}

		IEnumerable<ClientMsgs.Message> TilesToMessages(Dictionary<Environment, IEnumerable<IntPoint3D>> revealedLocs)
		{
			var msgs = revealedLocs.Where(kvp => kvp.Value.Count() > 0).
				Select(kvp => (ClientMsgs.Message)new ClientMsgs.TerrainData()
				{
					Environment = kvp.Key.ObjectID,
					TileDataList = kvp.Value.Select(l =>
						new KeyValuePair<IntPoint3D, TileData>(l, kvp.Key.GetTileData(l))
						).ToArray(),
					// XXX there seems to be a problem serializing this.
					// evaluating it with ToArray() fixes it
				});

			return msgs;
		}

		IEnumerable<ClientMsgs.Message> ObjectsToMessages(IEnumerable<ServerGameObject> revealedObs)
		{
			var msgs = revealedObs.Select(o => o.Serialize());
			return msgs;
		}
	}
}
