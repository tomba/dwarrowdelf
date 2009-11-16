using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MyGame
{
	public class ServerService
	{
		static int s_userIDs = 1;

		World m_world;
		Living m_player;

		int m_userID;

		// this user sees all
		bool m_seeAll = true;

		List<Living> m_friendlies = new List<Living>();

		ServerConnection m_connection;

		public ServerService(ServerConnection connection)
		{
			MyDebug.WriteLine("New ServerService");
			m_connection = connection;
			//m_client = OperationContext.Current.GetCallbackChannel<IClientCallback>();
		}

		public void Stop()
		{
			m_connection = null;
		}

		#region IServerService Members

		public void LogOn(string name)
		{
			m_world = World.TheWorld;

			m_world.BeginInvoke(_LogOn, name);
		}

		void _LogOn(object data)
		{
			string name = (string)data;

			MyDebug.WriteLine("LogOn {0}", name);

			m_userID = s_userIDs++;

			m_connection.Send(new ClientMsgs.LogOnReply() { UserID = m_userID });

			if (m_seeAll)
			{
				foreach (var env in m_world.Environments)
				{
					var msg = env.Serialize();
					m_connection.Send(msg);
				}
			}

			m_world.HandleChangesEvent += HandleChanges;
			m_world.HandleEventsEvent += HandleEvents;
		}

		public void LogOff()
		{
			m_world.BeginInvoke(_LogOff);
		}

		void _LogOff(object data)
		{
			MyDebug.WriteLine("Logout");

			if (m_player != null)
				_LogOffChar(null);

			m_world.HandleChangesEvent -= HandleChanges;
			m_world.HandleEventsEvent -= HandleEvents;

			m_world = null;
		}

		public void SetTiles(ObjectID mapID, IntCube cube, InteriorID type)
		{
			m_world.BeginInvokeInstant(_SetTiles, new object[] { mapID, cube, type });
		}

		void _SetTiles(object data)
		{
			object[] arr = (object[])data;
			ObjectID mapID = (ObjectID)arr[0];
			IntCube r = (IntCube)arr[1];
			InteriorID type = (InteriorID)arr[2];

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			foreach (var p in r.Range())
			{
				if (!env.Bounds.Contains(p))
					continue;

				env.SetInteriorID(p, type);
			}
		}

		public void ProceedTurn()
		{
			m_world.BeginInvoke(_ProceedTurn);
		}

		public void _ProceedTurn(object data)
		{
			MyDebug.WriteLine("ProceedTurn command");
			m_world.RequestTurn();
		}

		/* functions for livings */
		public void LogOnChar(string name)
		{
			m_world.BeginInvoke(_LogOnChar, name);
		}

		public void _LogOnChar(object data)
		{
			string name = (string)data;

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
			m_connection.Send(new ClientMsgs.LogOnCharReply() { PlayerID = m_player.ObjectID });


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
			m_connection.Send(inv);

			var pet = new Living(m_world);
			pet.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
			pet.Name = "lemmikki";
			pet.Actor = new InteractiveActor();
			m_friendlies.Add(pet);
			m_connection.Send(new ClientMsgs.LogOnCharReply() { PlayerID = pet.ObjectID });

			pet.MoveTo(m_player.Environment, m_player.Location + new IntVector(1, 0));
		}

		public void LogOffChar()
		{
			m_world.BeginInvokeInstant(_LogOffChar);
		}

		void _LogOffChar(object data)
		{
			m_player.EnqueueAction(new WaitAction(1));
			m_world.BeginInvoke(__LogOffChar);
		}

		void __LogOffChar(object data)
		{
			MyDebug.WriteLine("LogOffChar");

			m_friendlies.Remove(m_player);

			m_world.RemoveUser(this);

			m_player.Actor = null;
			m_player.Cleanup();
			m_player = null;

			m_connection.Send(new ClientMsgs.LogOffCharReply());
		}

		public void EnqueueAction(GameAction action)
		{
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

		#endregion


		void HandleEvents(IEnumerable<Event> events)
		{
			events = events.Where(e => EventFilter(e));

			var msgs = events.Select(e => (ClientMsgs.Message)new ClientMsgs.EventMessage(e));

			m_connection.Send(msgs);
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
				m_connection.Send(msgs);
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
					MapDataList = new ClientMsgs.MapTileData[] {
						new ClientMsgs.MapTileData() { Location = mc.Location, TileData = mc.TerrainType }
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
							ObjectID = l.Environment.ObjectID,
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
					MapDataList = kvp.Value.Select(l =>
						new ClientMsgs.MapTileData()
						{
							Location = l,
							TileData = new TileIDs()
							{
								m_interiorID = kvp.Key.GetInteriorID(l),
								m_floorID = kvp.Key.GetFloorID(l),
							}
						}).ToArray()
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
