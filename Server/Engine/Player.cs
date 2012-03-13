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
	[SaveGameObjectByRef]
	public sealed class Player : IPlayer
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

		IConnection m_connection;
		public bool IsConnected { get { return m_connection != null; } }

		public World World { get { return m_world; } }

		[SaveGameProperty("UserID")]
		int m_userID;
		public int UserID { get { return m_userID; } }

		// does this player sees all
		[SaveGameProperty("SeeAll")]
		bool m_seeAll;

		public bool IsSeeAll { get { return m_seeAll; } }

		[SaveGameProperty("Controllables")]
		List<LivingObject> m_controllables;
		public ReadOnlyCollection<LivingObject> Controllables { get; private set; }

		public bool IsController(BaseObject living) { return this.Controllables.Contains(living); }

		IPRunner m_ipRunner;

		ChangeHandler m_changeHandler;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		bool IsProceedTurnRequestSent { get; set; }
		public bool IsProceedTurnReplyReceived { get; private set; }
		public event Action<Player> ProceedTurnReceived;

		public event Action<Player> DisconnectEvent;

		public Player(int userID)
		{
			m_userID = userID;
			m_seeAll = false;

			m_controllables = new List<LivingObject>();
			this.Controllables = new ReadOnlyCollection<LivingObject>(m_controllables);

			if (m_seeAll)
				m_changeHandler = new AdminChangeHandler(this);
			else
				m_changeHandler = new PlayerChangeHandler(this);
		}

		Player(SaveGameContext ctx)
		{
			this.Controllables = new ReadOnlyCollection<LivingObject>(m_controllables);

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

			// XXX creating IP engine takes some time. Do it in the background. Race condition with IP msg handlers
			System.Threading.Tasks.Task.Factory.StartNew(delegate
			{
				m_ipRunner = new IPRunner(m_world, Send);
			});
		}

		public void Connect(IConnection connection)
		{
			Debug.Assert(m_connection == null);

			m_world.HandleMessagesEvent += HandleNewMessages;
			m_world.WorldChanged += HandleWorldChange;
			m_world.ReportReceived += HandleReport;

			m_connection = connection;
			m_connection.Start(_OnReceiveMessage, _OnDisconnect);


			Send(new Messages.LogOnReplyBeginMessage()
			{
				IsSeeAll = this.IsSeeAll,
				Tick = m_world.TickNumber,
				LivingVisionMode = m_world.LivingVisionMode,
			});

			if (m_seeAll)
			{
				// Send all objects without a parent. Those with a parent will be sent in the inventories of the parents
				foreach (var ob in this.World.AllObjects)
				{
					var sob = ob as MovableObject;

					if (sob == null || sob.Parent == null)
						ob.SendTo(this, ObjectVisibility.All);
				}
			}

			SendControllables();

			Send(new Messages.LogOnReplyEndMessage()
			{
				ClientData = m_engine.LoadClientData(this.UserID, m_engine.LastLoadID),
			});

			if (this.World.IsTickOnGoing)
			{
				if (this.World.TickMethod == WorldTickMethod.Simultaneous)
					SendProceedTurnRequest(null);
				else
					throw new NotImplementedException();
			}
		}

		void HandleDisconnect()
		{
			m_world.HandleMessagesEvent -= HandleNewMessages;
			m_world.WorldChanged -= HandleWorldChange;
			m_world.ReportReceived -= HandleReport;

			m_connection = null;

			this.IsProceedTurnRequestSent = false;
			this.IsProceedTurnReplyReceived = false;

			if (DisconnectEvent != null)
				DisconnectEvent(this);
		}

		public void Disconnect()
		{
			m_connection.Disconnect();
		}

		void _OnDisconnect()
		{
			trace.TraceInformation("OnDisconnect");
			m_engine.SignalWorld();
		}

		void _OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage");
			m_msgQueue.Enqueue(m);
			m_engine.SignalWorld();
		}

		System.Collections.Concurrent.ConcurrentQueue<Message> m_msgQueue = new System.Collections.Concurrent.ConcurrentQueue<Message>();

		public void HandleNewMessages()
		{
			trace.TraceVerbose("HandleNewMessages, count = {0}", m_msgQueue.Count);

			Message msg;
			while (m_msgQueue.TryDequeue(out msg))
				OnReceiveMessage(msg);

			if (!m_connection.IsConnected)
			{
				trace.TraceInformation("HandleNewMessages, disconnected");

				HandleDisconnect();
			}
		}

		public void AddControllable(LivingObject living)
		{
			m_controllables.Add(living);
			living.Destructed += OnControllableDestructed;

			if (this.IsConnected)
			{
				// Always send object data when the living has became a controllable
				living.SendTo(this, ObjectVisibility.All);

				Send(new Messages.ControllablesDataMessage()
				{
					Operation = ControllablesDataMessage.Op.Add,
					Controllables = new ObjectID[] { living.ObjectID }
				});

				// If the new controllable is in an environment, inform the vision tracker about this so it can update the vision data
				if (living.Environment != null)
				{
					var tracker = GetVisionTrackerInternal(living.Environment);
					tracker.HandleNewControllable(living);
				}
			}
		}

		void RemoveControllable(LivingObject living)
		{
			var ok = m_controllables.Remove(living);
			Debug.Assert(ok);
			living.Destructed -= OnControllableDestructed;
			Send(new Messages.ControllablesDataMessage()
			{
				Operation = ControllablesDataMessage.Op.Remove,
				Controllables = new ObjectID[] { living.ObjectID }
			});
		}

		void SendControllables()
		{
			Debug.Assert(this.IsConnected);

			foreach (var living in this.Controllables)
				living.SendTo(this, ObjectVisibility.All);

			Send(new Messages.ControllablesDataMessage()
			{
				Operation = ControllablesDataMessage.Op.Add,
				Controllables = this.Controllables.Select(l => l.ObjectID).ToArray()
			});

			foreach (var living in this.Controllables)
			{
				if (living.Environment != null)
				{
					var tracker = GetVisionTrackerInternal(living.Environment);
					tracker.HandleNewControllable(living);
				}
			}
		}

		void OnControllableDestructed(IBaseObject ob)
		{
			var living = (LivingObject)ob;
			RemoveControllable(living);
		}

		public void Send(ClientMessage msg)
		{
			if (m_connection != null)
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

		void ReceiveMessage(LogOutRequestMessage msg)
		{
			Send(new Messages.LogOutReplyMessage());

			foreach (var kvp in m_visionTrackers)
				kvp.Value.Stop();
			m_visionTrackers.Clear();

			m_connection.Disconnect();
		}

		void ReceiveMessage(CreateLivingMessage msg)
		{
			var controllables = new List<LivingObject>();

			Dwarrowdelf.AI.Group group = null;

			if (msg.IsGroup)
				group = new Dwarrowdelf.AI.Group();

			var livingInfo = Livings.GetLivingInfo(msg.LivingID);

			var env = this.World.FindObject<EnvironmentObject>(msg.EnvironmentID);

			if (env == null)
				throw new Exception();

			foreach (var p in msg.Area.Range())
			{
				var livingBuilder = new LivingObjectBuilder(msg.LivingID)
				{
					Name = msg.Name,
				};
				var living = livingBuilder.Create(this.World);

				if (msg.IsControllable)
				{
					m_engine.Game.Area.SetupLivingAsControllable(living);
				}
				else
				{
					switch (livingInfo.Category)
					{
						case LivingCategory.Herbivore:
							{
								var ai = new Dwarrowdelf.AI.HerbivoreAI(living);
								living.SetAI(ai);

								if (msg.IsGroup)
									ai.Group = group;
							}
							break;

						case LivingCategory.Carnivore:
							{
								var ai = new Dwarrowdelf.AI.CarnivoreAI(living);
								living.SetAI(ai);
							}
							break;

						case LivingCategory.Monster:
							{
								var ai = new Dwarrowdelf.AI.MonsterAI(living);
								living.SetAI(ai);
							}
							break;
					}
				}

				trace.TraceInformation("Created living {0}", living);

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

		Random m_random = new Random();
		IntPoint3 GetRandomSurfaceLocation(EnvironmentObject env, int zLevel)
		{
			IntPoint3 p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3(m_random.Next(env.Width), m_random.Next(env.Height), zLevel);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}

		/* functions for livings */
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
						if (living.ActionPriority == ActionPriority.High)
							throw new Exception();

						living.CancelAction();

						continue;
					}

					if (living.HasAction)
					{
						if (living.ActionPriority <= ActionPriority.User)
							living.CancelAction();
						else
							throw new Exception("already has an action");
					}

					living.StartAction(action, ActionPriority.User, m_userID);
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

		void ReceiveMessage(IPExpressionMessage msg)
		{
			trace.TraceInformation("IPExpressionMessage {0}", msg.Script);

			m_ipRunner.ExecExpr(msg.Script);
		}

		void ReceiveMessage(IPScriptMessage msg)
		{
			trace.TraceInformation("IPScriptMessage {0}", msg.Script);

			m_ipRunner.ExecScript(msg.Script, msg.Args);
		}

		void ReceiveMessage(SaveRequestMessage msg)
		{
			m_engine.Save();
		}

		void ReceiveMessage(SaveClientDataReplyMessage msg)
		{
			m_engine.SaveClientData(this.UserID, msg.ID, msg.Data);
		}


		void HandleReport(GameReport report)
		{
			if (report is LivingReport)
			{
				var r = (LivingReport)report;

				if (Sees((EnvironmentObject)r.Living.Environment, r.Living.Location))
					Send(new ReportMessage() { Report = report });
			}
			else
			{
				Send(new ReportMessage() { Report = report });
			}
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

		void SendProceedTurnRequest(LivingObject living)
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

		Dictionary<EnvironmentObject, VisionTrackerBase> m_visionTrackers = new Dictionary<EnvironmentObject, VisionTrackerBase>();

		public ObjectVisibility GetObjectVisibility(BaseObject ob)
		{
			var mo = ob as MovableObject;

			if (mo == null)
			{
				if ((ob is BuildingObject) == false)
					throw new Exception();

				// XXX If the ob is not movable object, it's a building. Send all.
				return ObjectVisibility.All;
			}

			for (MovableObject o = mo; o != null; o = o.Parent as MovableObject)
			{
				if (this.IsController(o))
				{
					// if this player is the controller of the object or any of the object's parent
					return ObjectVisibility.All;
				}
			}

			if (Sees(mo.Parent, mo.Location))
				return ObjectVisibility.Public
					| ObjectVisibility.Debug; // XXX debug also
			else
				return ObjectVisibility.None;
		}

		/// <summary>
		/// Does the player see location p in object ob
		/// </summary>
		public bool Sees(BaseObject ob, IntPoint3 p)
		{
			if (m_seeAll)
				return true;

			var env = ob as EnvironmentObject;
			if (env != null)
			{
				IVisionTracker tracker = GetVisionTracker(env);
				return tracker.Sees(p);
			}

			/* is it inside one of the controllables? */
			var lgo = ob as ConcreteObject;
			while (lgo != null)
			{
				if (m_controllables.Contains(lgo))
					return true;

				lgo = lgo.Parent as ConcreteObject;
			}

			return false;
		}

		public IVisionTracker GetVisionTracker(EnvironmentObject env)
		{
			return GetVisionTrackerInternal(env);
		}

		VisionTrackerBase GetVisionTrackerInternal(EnvironmentObject env)
		{
			// XXX we send the initial mapdata in tracker.Start(). So if the player is not connected yet, we don't send the mapdata
			if (!IsConnected)
				throw new Exception();

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

	sealed class AdminChangeHandler : ChangeHandler
	{
		public AdminChangeHandler(Player player)
			: base(player)
		{
		}

		public override void HandleWorldChange(Change change)
		{
			var changeMsg = new ChangeMessage() { ChangeData = change.ToChangeData() };

			Send(changeMsg);

			if (change is ObjectCreatedChange)
			{
				var c = (ObjectCreatedChange)change;
				var newObject = c.Object;
				newObject.SendTo(m_player, ObjectVisibility.All);
			}
		}
	}

	sealed class PlayerChangeHandler : ChangeHandler
	{
		public PlayerChangeHandler(Player player)
			: base(player)
		{

		}

		public override void HandleWorldChange(Change change)
		{
			// XXX if the created object cannot be moved (i.e. not ServerGameObject), we need to send the object data manually here.
			// f.ex. buildings
			var occ = change as ObjectCreatedChange;
			if (occ != null)
			{
				if (!(occ.Object is MovableObject))
				{
					occ.Object.SendTo(m_player, ObjectVisibility.All);
				}
			}

			// can the player see the change?
			if (!CanSeeChange(change, m_player.Controllables))
				return;

			{
				// We don't collect newly visible terrains/objects on AllVisible maps.
				// However, we still need to tell about newly created objects that come
				// to AllVisible maps.
				var c = change as ObjectMoveChange;
				if (c != null && c.Source != c.Destination && c.Destination is EnvironmentObject &&
					(((EnvironmentObject)c.Destination).VisibilityMode == VisibilityMode.AllVisible || ((EnvironmentObject)c.Destination).VisibilityMode == VisibilityMode.GlobalFOV))
				{
					var newObject = c.Object;
					var vis = m_player.GetObjectVisibility(newObject);
					Debug.Assert(vis != ObjectVisibility.None);
					newObject.SendTo(m_player, vis);
				}
			}

			// When an armor is worn by a non-controllable, the armor isn't known to the client. Thus we need to send the data of the armor here.
			if (change is WearArmorChange)
			{
				var c = (WearArmorChange)change;

				if (m_player.IsController(c.Object) == false)
				{
					if (c.Wearable != null)
						c.Wearable.SendTo(m_player, ObjectVisibility.Public);
					// else it's being removed
				}
			}

			// The same for weapons
			if (change is WieldWeaponChange)
			{
				var c = (WieldWeaponChange)change;

				if (m_player.IsController(c.Object) == false)
				{
					if (c.Weapon != null)
						c.Weapon.SendTo(m_player, ObjectVisibility.Public);
					// else it's being removed
				}
			}

			var changeMsg = new ChangeMessage() { ChangeData = change.ToChangeData() };

			Send(changeMsg);
		}

		bool CanSeeChange(Change change, IList<LivingObject> controllables)
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
				var env = ((MovableObject)c.Object).Parent;

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
					var ov = m_player.GetObjectVisibility(c.Object);

					if ((ov & vis) == 0)
						return false;

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

			else if (change is ObjectChange)
			{
				var c = (ObjectChange)change;

				var vis = m_player.GetObjectVisibility(c.Object);

				return vis != ObjectVisibility.None;
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
