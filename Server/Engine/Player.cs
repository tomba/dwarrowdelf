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
	public class Player : INotifyPropertyChanged, IPlayer
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

		[SaveGameProperty("Controllables")]
		List<Living> m_controllables;
		public ReadOnlyCollection<Living> Controllables { get; private set; }

		public bool IsController(IBaseGameObject living) { return this.Controllables.Contains(living); }
		public bool IsFriendly(IBaseGameObject living) { return m_seeAll || this.Controllables.Contains(living); }

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

			if (m_seeAll)
				m_changeHandler = new AdminChangeHandler(this);
			else
				m_changeHandler = new PlayerChangeHandler(this);
		}

		protected Player(SaveGameContext ctx)
		{
			this.Controllables = new ReadOnlyCollection<Living>(m_controllables);

			foreach (var l in this.Controllables)
				l.Destructed += OnControllableDestructed; // XXX remove if player deleted

			if (m_seeAll)
				m_changeHandler = new AdminChangeHandler(this);
			else
				m_changeHandler = new PlayerChangeHandler(this);
		}

		public void Init(GameEngine engine)
		{
			m_engine = engine;
			m_world = m_engine.World;

			trace.Header = String.Format("Player({0})", m_userID);
		}

		void AddControllable(Living living)
		{
			m_controllables.Add(living);
			living.Destructed += OnControllableDestructed;

			Send(new Messages.ControllablesDataMessage() { Operation = ControllablesDataMessage.Op.Add, Controllables = new ObjectID[] { living.ObjectID } });

			// Always send object data after the living has became a controllable
			living.SendTo(this, ObjectVisibility.All);

			// If the new controllable is in an environment, inform the vision tracker about this so it can update the vision data
			if (living.Environment != null)
			{
				var tracker = GetVisionTrackerInternal(living.Environment);
				tracker.HandleNewControllable(living);
			}
		}

		void RemoveControllable(Living living)
		{
			var ok = m_controllables.Remove(living);
			Debug.Assert(ok);
			living.Destructed -= OnControllableDestructed;
			Send(new Messages.ControllablesDataMessage() { Operation = ControllablesDataMessage.Op.Remove, Controllables = new ObjectID[] { living.ObjectID } });
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
					m_world.WorldChanged += HandleWorldChange;
					m_ipRunner = new IPRunner(m_world, Send);
				}
				else
				{
					m_world.WorldChanged -= HandleWorldChange;
					m_ipRunner = null;

					this.IsInGame = false;
				}

				Notify("IsConnected");
			}
		}

		void OnControllableDestructed(BaseGameObject ob)
		{
			var living = (Living)ob;
			RemoveControllable(living);
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

		public void ReceiveLogOnMessage(LogOnRequestMessage msg)
		{
			Send(new Messages.LogOnReplyBeginMessage() { IsSeeAll = m_seeAll, Tick = m_engine.World.TickNumber, LivingVisionMode = m_engine.World.LivingVisionMode, });

			if (m_seeAll)
			{
				// Send all objects without a parent. Those with a parent will be sent in the inventories of the parents
				foreach (var ob in this.World.AllObjects)
				{
					var sob = ob as GameObject;

					if (sob == null || sob.Parent == null)
						ob.SendTo(this, ObjectVisibility.All);
				}
			}

			Send(new Messages.LogOnReplyEndMessage());
		}

		public void ReceiveLogOutMessage(LogOutRequestMessage msg)
		{
			Send(new Messages.LogOutReplyMessage());

			foreach (var kvp in m_visionTrackers)
				kvp.Value.Stop();
			m_visionTrackers.Clear();
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

			var env = m_world.FindObject<Environment>(mapID);
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

		void ReceiveMessage(CreateItemMessage msg)
		{
			var builder = new ItemObjectBuilder(msg.ItemID, msg.MaterialID);
			var item = builder.Create(this.World);

			trace.TraceInformation("Created item {0}", item);

			if (msg.EnvironmentID != ObjectID.NullObjectID)
			{
				var env = this.World.FindObject<Environment>(msg.EnvironmentID);

				if (env == null)
					throw new Exception();

				if (msg.Location.HasValue)
					item.MoveTo(env, msg.Location.Value);
				else
					item.MoveTo(env);
			}
		}

		void ReceiveMessage(CreateLivingMessage msg)
		{
			bool useNum = msg.Area.Area > 1;
			int num = 0;
			var controllables = new List<Living>();

			Dwarrowdelf.AI.HerbivoreHerd herd = null;

			if (msg.IsHerd)
				herd = new Dwarrowdelf.AI.HerbivoreHerd();

			foreach (var p in msg.Area.Range())
			{
				string name = useNum ? String.Format("{0} {1}", msg.Name, num++) : msg.Name;

				var livingBuilder = new LivingBuilder(msg.LivingID)
				{
					Name = msg.Name,
				};
				var living = livingBuilder.Create(this.World);

				if (msg.IsControllable)
					m_engine.SetupControllable(living);
				else
				{
					var ai = new Dwarrowdelf.AI.HerbivoreAI(living);
					living.SetAI(ai);

					if (msg.IsHerd)
						herd.AddMember(ai);
				}

				trace.TraceInformation("Created living {0}", living);

				var env = this.World.FindObject<Environment>(msg.EnvironmentID);

				if (env == null)
					throw new Exception();

				living.MoveTo(env, p);

				if (msg.IsControllable)
					controllables.Add(living);
			}

			foreach (var l in controllables)
				AddControllable(l);
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

			var env = m_world.FindObject<Environment>(mapID);
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
				Send(new Messages.ControllablesDataMessage() { Operation = ControllablesDataMessage.Op.Add, Controllables = this.Controllables.Select(l => l.ObjectID).ToArray() });
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

			Send(new Messages.EnterGameReplyBeginMessage());

			if (!m_hasControllablesBeenCreated)
			{
				trace.TraceInformation("Creating controllables");
				var controllables = m_engine.CreateControllables(this);
				foreach (var l in controllables)
					AddControllable(l);

				m_hasControllablesBeenCreated = true;
			}

			Send(new Messages.EnterGameReplyEndMessage() { ClientData = m_engine.LoadClientData(this.UserID, m_engine.LastLoadID) });

			this.IsInGame = true;
		}

		void ReceiveMessage(ExitGameRequestMessage msg)
		{
			trace.TraceInformation("ExitGameRequestMessage");

			Send(new Messages.ControllablesDataMessage() { Operation = ControllablesDataMessage.Op.Remove, Controllables = this.Controllables.Select(l => l.ObjectID).ToArray() });
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

					var living = this.Controllables.SingleOrDefault(l => l.ObjectID == actorOid);

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
				if (!this.IsController(c.Living))
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

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		Dictionary<Environment, VisionTrackerBase> m_visionTrackers = new Dictionary<Environment, VisionTrackerBase>();

		public ObjectVisibility GetObjectVisibility(IBaseGameObject ob)
		{
			var sgo = ob as GameObject;

			if (sgo == null)
			{
				// XXX If the ob is not SGO, it's a building. Send all.
				return ObjectVisibility.All;
			}

			for (GameObject o = sgo; o != null; o = o.Parent)
			{
				if (this.IsController(o))
				{
					// if this player is the controller of the object or any of the object's parent
					return ObjectVisibility.All;
				}
			}

			if (Sees(sgo.Parent, sgo.Location))
				return ObjectVisibility.Public;
			else
				return ObjectVisibility.None;
		}

		public bool Sees(IBaseGameObject ob, IntPoint3D p)
		{
			if (m_seeAll)
				return true;

			var env = ob as Environment;

			if (env == null)
				return false;

			IVisionTracker tracker = GetVisionTracker(env);

			return tracker.Sees(p);
		}

		public IVisionTracker GetVisionTracker(IEnvironment env)
		{
			return GetVisionTrackerInternal((Environment)env);
		}

		VisionTrackerBase GetVisionTrackerInternal(Environment env)
		{
			if (m_seeAll)
				return AdminVisionTracker.Tracker;

			VisionTrackerBase tracker;

			if (m_visionTrackers.TryGetValue(env, out tracker) == false)
			{
				switch (env.VisibilityMode)
				{
					case VisibilityMode.AllVisible:
						tracker = new AllVisibleVisionTracker(this, env);
						break;

					case VisibilityMode.GlobalFOV:
						tracker = new GlobalFOVVisionTracker(this, env);
						break;

					case VisibilityMode.LivingLOS:
						tracker = new LOSVisionTracker(this, env);
						break;

					default:
						throw new NotImplementedException();
				}

				m_visionTrackers[env] = tracker;

				tracker.Start();
			}

			return tracker;
		}
	}

	abstract class ChangeHandler
	{
		protected Player m_player;

		protected ChangeHandler(Player player)
		{
			m_player = player;
		}

		protected void Send(ClientMessage msg)
		{
			m_player.Send(msg);
		}

		protected void Send(IEnumerable<ClientMessage> msgs)
		{
			m_player.Send(msgs);
		}

		public abstract void HandleWorldChange(Change change);
	}

	class AdminChangeHandler : ChangeHandler
	{
		public AdminChangeHandler(Player player)
			: base(player)
		{
		}

		public override void HandleWorldChange(Change change)
		{
			var changeMsg = new ChangeMessage { Change = change };

			Send(changeMsg);

			if (change is ObjectCreatedChange)
			{
				var c = (ObjectCreatedChange)change;
				var newObject = (BaseGameObject)c.Object;
				newObject.SendTo(m_player, ObjectVisibility.All);
			}
		}
	}

	class PlayerChangeHandler : ChangeHandler
	{
		public PlayerChangeHandler(Player player)
			: base(player)
		{

		}

		public override void HandleWorldChange(Change change)
		{
			// XXX if the created object cannot be moved (i.e. not ServerGameObject), we need to send the object data manually here
			var occ = change as ObjectCreatedChange;
			if (occ != null)
			{
				if (!(occ.Object is GameObject))
				{
					var newObject = (BaseGameObject)occ.Object;
					newObject.SendTo(m_player, ObjectVisibility.All);
				}
			}

			// can the player see the change?
			if (!CanSeeChange(change, m_player.Controllables))
				return;

			// We don't collect newly visible terrains/objects on AllVisible maps.
			// However, we still need to tell about newly created objects that come
			// to AllVisible maps.
			var c = change as ObjectMoveChange;
			if (c != null && c.Source != c.Destination && c.Destination is Environment &&
				(((Environment)c.Destination).VisibilityMode == VisibilityMode.AllVisible || ((Environment)c.Destination).VisibilityMode == VisibilityMode.GlobalFOV))
			{
				var newObject = (GameObject)c.Object;
				var vis = m_player.GetObjectVisibility(newObject);
				newObject.SendTo(m_player, vis);
			}

			var changeMsg = new ChangeMessage { Change = change };

			Send(changeMsg);
		}

		bool CanSeeChange(Change change, IList<Living> controllables)
		{
			if (change is TurnStartSimultaneousChange || change is TurnEndSimultaneousChange)
			{
				return true;
			}
			else if (change is TurnStartSequentialChange || change is TurnEndSequentialChange)
			{
				return true;
			}
			else if (change is TickStartChange)
			{
				return true;
			}
			else if (change is ObjectDestructedChange)
			{
				// XXX We should only send this if the player sees the object.
				// And the client should have a cleanup of some kind to remove old objects (which may or may not be destructed)
				return true;
			}
			else if (change is ObjectCreatedChange)
			{
				return false;
			}
			else if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;

				if (controllables.Contains(c.Object))
					return true;

				if (m_player.Sees(c.Source, c.SourceLocation))
					return true;

				if (m_player.Sees(c.Destination, c.DestinationLocation))
					return true;

				return false;
			}
			else if (change is ObjectMoveLocationChange)
			{
				var c = (ObjectMoveLocationChange)change;

				if (controllables.Contains(c.Object))
					return true;

				// XXX
				var env = ((IGameObject)c.Object).Parent;

				if (m_player.Sees(env, c.SourceLocation))
					return true;

				if (m_player.Sees(env, c.DestinationLocation))
					return true;

				return false;
			}
			else if (change is MapChange)
			{
				var c = (MapChange)change;
				return m_player.Sees(c.Environment, c.Location);
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
					var vis = PropertyVisibilities.GetPropertyVisibility(c.PropertyID);

					if (vis == PropertyVisibility.Friendly && !m_player.IsFriendly(c.Object))
						return false;

					GameObject sob = c.Object as GameObject;
					if (sob != null)
						return m_player.Sees(sob.Environment, sob.Location);

					// XXX how to check for non-ServerGameObjects? for example building objects
					return true;
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

			else
			{
				throw new Exception();
			}
		}
	}
}
