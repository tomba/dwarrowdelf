using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Dwarrowdelf.Messages;
using System.ComponentModel;

namespace Dwarrowdelf.Server
{
	[SaveGameObject]
	public class Player : INotifyPropertyChanged
	{
		static Dictionary<Type, Action<Player, ServerMessage>> s_handlerMap;

		static Player()
		{
			var messageTypes = Helpers.GetNonabstractSubclasses(typeof(ServerMessage));

			s_handlerMap = new Dictionary<Type, Action<Player, ServerMessage>>(messageTypes.Count());

			foreach (var type in messageTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<Player, ServerMessage>("ReceiveMessage", type);
				if (method != null)
					s_handlerMap[type] = method;
			}
		}

		GameEngine m_engine;
		World m_world;

		ServerConnection m_connection;

		public World World { get { return m_world; } }

		[SaveGameProperty("HasControllablesBeenCreated")]
		bool m_hasControllablesBeenCreated;

		bool m_isInGame;
		public bool IsInGame
		{
			get { return m_isInGame; }

			private set
			{
				if (m_isInGame == value)
					return;

				m_isInGame = value;
				Notify("IsInGame");
			}
		}

		[SaveGameProperty("UserID")]
		int m_userID;
		public int UserID { get { return m_userID; } }

		// does this player sees all
		[SaveGameProperty("SeeAll")]
		bool m_seeAll;
		public bool IsSeeAll { get { return m_seeAll; } }

		[SaveGameProperty("Controllables")]
		List<Living> m_controllables;
		public ReadOnlyCollection<Living> Controllables { get; private set; }

		IPRunner m_ipRunner;

		ChangeHandler m_changeHandler;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		bool IsProceedTurnRequestSent { get; set; }
		public bool IsProceedTurnReplyReceived { get; private set; }
		public event Action<Player> ProceedTurnReceived;

		public Player(int userID)
		{
			m_userID = userID;
			m_seeAll = true;

			m_controllables = new List<Living>();
			this.Controllables = new ReadOnlyCollection<Living>(m_controllables);
			m_changeHandler = new ChangeHandler(this);
		}

		protected Player(SaveGameContext ctx)
		{
			this.Controllables = new ReadOnlyCollection<Living>(m_controllables);

			foreach (var l in this.Controllables)
				l.Destructed += OnPlayerDestructed; // XXX remove if player deleted

			m_changeHandler = new ChangeHandler(this);
		}

		public void Init(GameEngine engine)
		{
			m_engine = engine;
			m_world = m_engine.World;

			trace.Header = String.Format("Player({0})", m_userID);
		}

		public bool IsConnected { get { return m_connection != null; } }

		public ServerConnection Connection
		{
			get { return m_connection; }
			set
			{
				if (m_connection == value)
					return;

				m_connection = value;

				if (m_connection != null)
				{
					m_world.WorkEnded += HandleEndOfWork;
					m_world.WorldChanged += HandleWorldChange;
					m_ipRunner = new IPRunner(m_world, Send);
				}
				else
				{
					m_world.WorkEnded -= HandleEndOfWork;
					m_world.WorldChanged -= HandleWorldChange;
					m_ipRunner = null;

					this.IsInGame = false;
				}

				Notify("IsConnected");
			}
		}

		void OnPlayerDestructed(BaseGameObject ob)
		{
			var living = (Living)ob;
			m_controllables.Remove(living);
			living.Destructed -= OnPlayerDestructed;
			Send(new Messages.ControllablesDataMessage() { Controllables = m_controllables.Select(l => l.ObjectID).ToArray() });
		}

		public void Send(ClientMessage msg)
		{
			m_connection.Send(msg);
		}

		public void Send(IEnumerable<ClientMessage> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}

		public void OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage({0})", m);

			var msg = (ServerMessage)m;

			Action<Player, ServerMessage> method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void ReceiveMessage(SetTilesMessage msg)
		{
			ObjectID mapID = msg.MapID;
			IntCuboid r = msg.Cube;

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			foreach (var p in r.Range())
			{
				if (!env.Contains(p))
					continue;

				var tileData = env.GetTileData(p);

				if (msg.InteriorID.HasValue)
					tileData.InteriorID = msg.InteriorID.Value;
				if (msg.InteriorMaterialID.HasValue)
					tileData.InteriorMaterialID = msg.InteriorMaterialID.Value;

				if (msg.TerrainID.HasValue)
					tileData.TerrainID = msg.TerrainID.Value;
				if (msg.TerrainMaterialID.HasValue)
					tileData.TerrainMaterialID = msg.TerrainMaterialID.Value;

				if (msg.WaterLevel.HasValue)
					tileData.WaterLevel = msg.WaterLevel.Value;
				if (msg.Grass.HasValue)
					tileData.Grass = msg.Grass.Value;

				env.SetTileData(p, tileData);
			}

			env.ScanWaterTiles();
		}

		void ReceiveMessage(SetWorldConfigMessage msg)
		{
			if (msg.MinTickTime.HasValue)
				m_engine.SetMinTickTime(msg.MinTickTime.Value);
		}

		void ReceiveMessage(CreateBuildingMessage msg)
		{
			ObjectID mapID = msg.MapID;
			var r = msg.Area;
			var id = msg.ID;

			var env = m_world.Environments.SingleOrDefault(e => e.ObjectID == mapID);
			if (env == null)
				throw new Exception();

			if (BuildingObject.VerifyBuildSite(env, r) == false)
			{
				trace.TraceWarning("Bad site for building, ignoring CreateBuildingMessage");
				return;
			}

			var builder = new BuildingObjectBuilder(id, r);
			builder.Create(m_world, env);
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
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}

		/* functions for livings */
		void ReceiveMessage(EnterGameRequestMessage msg)
		{
			if (m_hasControllablesBeenCreated)
			{
				Send(new Messages.EnterGameReplyBeginMessage());
				Send(new Messages.ControllablesDataMessage() { Controllables = m_controllables.Select(l => l.ObjectID).ToArray() });
				Send(new Messages.EnterGameReplyEndMessage() { ClientData = m_engine.LoadClientData(this.UserID, m_engine.LastLoadID) });

				this.IsInGame = true;

				if (this.World.IsTickOnGoing)
				{
					if (this.World.TickMethod == WorldTickMethod.Simultaneous)
						SendProceedTurnRequest(null);
					else
						throw new NotImplementedException();
				}
			}
			else
			{
				m_world.BeginInvoke(new Action<EnterGameRequestMessage>(HandleEnterGame), msg);
			}
		}

		void HandleEnterGame(EnterGameRequestMessage msg)
		{
			string name = msg.Name;

			trace.TraceInformation("EnterGameRequestMessage {0}", name);

			if (!m_hasControllablesBeenCreated)
			{
				trace.TraceInformation("Creating controllables");
				var controllables = m_engine.CreateControllables(this);
				m_controllables.AddRange(controllables);

				foreach (var l in m_controllables)
					l.Destructed += OnPlayerDestructed;

				m_hasControllablesBeenCreated = true;
			}

			Send(new Messages.EnterGameReplyBeginMessage());
			Send(new Messages.ControllablesDataMessage() { Controllables = m_controllables.Select(l => l.ObjectID).ToArray() });
			Send(new Messages.EnterGameReplyEndMessage() { ClientData = m_engine.LoadClientData(this.UserID, m_engine.LastLoadID) });

			this.IsInGame = true;
		}

		void ReceiveMessage(ExitGameRequestMessage msg)
		{
			trace.TraceInformation("ExitGameRequestMessage");

			Send(new Messages.ControllablesDataMessage() { Controllables = new ObjectID[0] });
			Send(new Messages.ExitGameReplyMessage());

			this.IsInGame = false;
		}

		void ReceiveMessage(ProceedTurnReplyMessage msg)
		{
			try
			{
				if (this.IsProceedTurnRequestSent == false)
					throw new Exception();

				if (this.IsProceedTurnReplyReceived == true)
					throw new Exception();

				foreach (var tuple in msg.Actions)
				{
					var actorOid = tuple.Item1;
					var action = tuple.Item2;

					var living = m_controllables.SingleOrDefault(l => l.ObjectID == actorOid);

					if (living == null)
						continue;

					if (action == null)
					{
						if (living.CurrentAction.Priority == ActionPriority.High)
							throw new Exception();

						living.CancelAction();

						continue;
					}

					if (action.Priority > ActionPriority.Normal)
						throw new Exception();

					if (living.HasAction)
					{
						if (living.CurrentAction.Priority <= action.Priority)
							living.CancelAction();
						else
							throw new Exception("already has an action");
					}

					living.DoAction(action, m_userID);
				}

				this.IsProceedTurnReplyReceived = true;

				if (ProceedTurnReceived != null)
					ProceedTurnReceived(this);
			}
			catch (Exception e)
			{
				trace.TraceError("Uncaught exception");
				trace.TraceError(e.ToString());
			}
		}

		void ReceiveMessage(IPCommandMessage msg)
		{
			trace.TraceInformation("IronPythonCommand");

			m_ipRunner.Exec(msg.Text);
		}

		void ReceiveMessage(SaveRequestMessage msg)
		{
			m_engine.Save();
		}

		void ReceiveMessage(SaveClientDataReplyMessage msg)
		{
			m_engine.SaveClientData(this.UserID, msg.ID, msg.Data);
		}


		void HandleWorldChange(Change change)
		{
			m_changeHandler.HandleWorldChange(change);

			if (change is TurnStartSimultaneousChange)
			{
				SendProceedTurnRequest(null);
			}
			else if (change is TurnStartSequentialChange)
			{
				var c = (TurnStartSequentialChange)change;
				if (m_controllables.Contains(c.Living))
					return;

				SendProceedTurnRequest(c.Living);
			}
			else if (change is TurnEndSimultaneousChange || change is TurnEndSequentialChange)
			{
				this.IsProceedTurnRequestSent = false;
				this.IsProceedTurnReplyReceived = false;
			}
		}

		void SendProceedTurnRequest(ILiving living)
		{
			ObjectID id;

			if (living == null)
				id = ObjectID.AnyObjectID;
			else
				id = living.ObjectID;

			this.IsProceedTurnRequestSent = true;

			var msg = new ProceedTurnRequestMessage() { LivingID = id };
			Send(msg);
		}

		void HandleEndOfWork()
		{
			m_changeHandler.HandleEndOfWork();
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

	}



	class ChangeHandler
	{
		// These are used to determine new tiles and objects in sight
		HashSet<Environment> m_knownEnvironments = new HashSet<Environment>();

		Dictionary<Environment, HashSet<IntPoint3D>> m_oldKnownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_oldKnownObjects = new HashSet<ServerGameObject>();

		Dictionary<Environment, HashSet<IntPoint3D>> m_newKnownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_newKnownObjects = new HashSet<ServerGameObject>();

		Player m_player;

		public ChangeHandler(Player player)
		{
			m_player = player;
		}

		void Send(ClientMessage msg)
		{
			m_player.Send(msg);
		}

		void Send(IEnumerable<ClientMessage> msgs)
		{
			m_player.Send(msgs);
		}

		// Called from the world at the end of work
		public void HandleEndOfWork()
		{
			// if the player sees all, no need to send new terrains/objects
			if (!m_player.IsSeeAll)
				HandleNewTerrainsAndObjects(m_player.Controllables);
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
				if (l.Environment.VisibilityMode == VisibilityMode.AllVisible || l.Environment.VisibilityMode == VisibilityMode.GlobalFOV)
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
				if (env.VisibilityMode == VisibilityMode.AllVisible || env.VisibilityMode == VisibilityMode.GlobalFOV)
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
				Select(kvp => (Messages.ClientMessage)new Messages.MapDataTerrainsListMessage()
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
			var msgs = revealedObjects.Select(o => new ObjectDataMessage() { ObjectData = o.Serialize() });
			Send(msgs);
		}


		public void HandleWorldChange(Change change)
		{
			// can any friendly see the change?
			if (!m_player.IsSeeAll && !CanSeeChange(change, m_player.Controllables))
				return;

			if (!m_player.IsSeeAll)
			{
				// We don't collect newly visible terrains/objects on AllVisible maps.
				// However, we still need to tell about newly created objects that come
				// to AllVisible maps.
				var c = change as ObjectMoveChange;
				if (c != null && c.Source != c.Destination && c.Destination is Environment &&
					(((Environment)c.Destination).VisibilityMode == VisibilityMode.AllVisible || ((Environment)c.Destination).VisibilityMode == VisibilityMode.GlobalFOV))
				{
					var newObject = (ServerGameObject)c.Object;
					var newObMsg = ObjectToMessage(newObject);
					Send(newObMsg);
				}
			}

			var changeMsg = new ChangeMessage { Change = change };

			Send(changeMsg);

			// XXX this is getting confusing...
			if (m_player.IsSeeAll && change is ObjectCreatedChange)
			{
				var c = (ObjectCreatedChange)change;
				var newObject = (BaseGameObject)c.Object;
				var newObMsg = ObjectToMessage(newObject);
				Send(newObMsg);
			}
		}

		bool CanSeeChange(Change change, IList<Living> controllables)
		{
			// XXX these checks are not totally correct. objects may have changed after
			// the creation of the change, for example moved. Should changes contain
			// all the information needed for these checks?
			if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;

				return controllables.Any(l =>
				{
					if (l == c.Object)
						return true;

					if (l.Sees(c.Source, c.SourceLocation))
						return true;

					if (l.Sees(c.Destination, c.DestinationLocation))
						return true;

					return false;
				});
			}
			else if (change is MapChange)
			{
				var c = (MapChange)change;
				return controllables.Any(l => l.Sees(c.Environment, c.Location));
			}
			else if (change is TurnStartSimultaneousChange || change is TurnEndSimultaneousChange)
			{
				return true;
			}
			else if (change is TurnStartSequentialChange || change is TurnEndSequentialChange)
			{
				return true;
			}
			else if (change is FullObjectChange)
			{
				var c = (FullObjectChange)change;
				return controllables.Contains(c.Object);
			}
			else if (change is PropertyChange)
			{
				var c = (PropertyChange)change;

				if (controllables.Contains(c.Object))
				{
					return true;
				}
				else
				{
					// Should check if the property is public or not
					ServerGameObject ob = (ServerGameObject)c.Object;
					return controllables.Any(l => l.Sees(ob.Environment, ob.Location));
				}
			}
			else if (change is ActionStartedChange)
			{
				var c = (ActionStartedChange)change;
				return controllables.Contains(c.Object);
			}
			else if (change is ActionProgressChange)
			{
				var c = (ActionProgressChange)change;
				return controllables.Contains(c.Object);
			}
			else if (change is ActionDoneChange)
			{
				var c = (ActionDoneChange)change;
				return controllables.Contains(c.Object);
			}
			else if (change is TickStartChange || change is ObjectDestructedChange)
			{
				return true;
			}
			else if (change is ObjectCreatedChange)
			{
				return false;
			}
			else
			{
				throw new Exception();
			}
		}

		static ClientMessage ObjectToMessage(BaseGameObject revealedOb)
		{
			var msg = new ObjectDataMessage() { ObjectData = revealedOb.Serialize() };
			return msg;
		}
	}
}
