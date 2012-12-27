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

		public bool IsController(BaseObject living) { return m_controllables.Contains(living); }

		IPRunner m_ipRunner;

		ChangeHandler m_changeHandler;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public bool IsProceedTurnReplyReceived { get; private set; }
		public event Action<Player> ProceedTurnReceived;

		public event Action<Player> DisconnectEvent;

		public Player(int userID)
		{
			m_userID = userID;
			m_seeAll = false;

			m_controllables = new List<LivingObject>();

			Construct();
		}

		Player(SaveGameContext ctx)
		{
			InitControllables(m_controllables);

			Construct();
		}

		void Construct()
		{
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
				m_ipRunner = new IPRunner(m_world, m_engine, this);
			});
		}

		public void Connect(IConnection connection)
		{
			OnConnected(connection);
		}

		public void Disconnect()
		{
			m_connection.Disconnect();
		}

		void OnConnected(IConnection connection)
		{
			trace.TraceInformation("OnConnected");

			Debug.Assert(m_connection == null);

			m_world.WorldChanged += HandleWorldChange;
			m_world.ReportReceived += HandleReport;

			m_connection = connection;

			Send(new Messages.LogOnReplyBeginMessage()
			{
				IsSeeAll = this.IsSeeAll,
			});

			m_world.SendTo(this, this.IsSeeAll ? ObjectVisibility.All : ObjectVisibility.Public);

			InitControllablesVisionTracker(m_controllables);
			SendAddControllables(m_controllables);

			Send(new Messages.LogOnReplyEndMessage()
			{
				ClientData = m_engine.LoadClientData(this.UserID, m_engine.LastLoadID),
			});

			if (this.World.IsTickOnGoing)
			{
				if (this.World.TickMethod == WorldTickMethod.Simultaneous)
				{
					var change = new TurnStartChange(null);
					m_changeHandler.HandleWorldChange(change);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnected");

			m_connection = null;

			foreach (var c in m_controllables)
				UninitControllableVisionTracker(c);

			m_world.WorldChanged -= HandleWorldChange;
			m_world.ReportReceived -= HandleReport;

			this.IsProceedTurnReplyReceived = false;

			if (DisconnectEvent != null)
				DisconnectEvent(this);
		}

		// Called from game engine when creating new player, before the player object is connected
		public void SetupControllablesForNewPlayer(IEnumerable<LivingObject> controllables)
		{
			AddControllables(controllables);
		}

		void AddControllables(IEnumerable<LivingObject> controllables)
		{
			m_controllables.AddRange(controllables);

			InitControllables(controllables);

			if (this.IsConnected)
			{
				InitControllablesVisionTracker(controllables);
				SendAddControllables(controllables);
			}
		}

		void RemoveControllable(LivingObject living)
		{
			if (this.IsConnected)
			{
				SendRemoveControllable(living);

				UninitControllableVisionTracker(living);
			}

			UninitControllable(living);

			var ok = m_controllables.Remove(living);
			Debug.Assert(ok);
		}

		void InitControllables(IEnumerable<LivingObject> controllables)
		{
			foreach (var c in controllables)
			{
				c.Destructed += OnControllableDestructed;
				c.Controller = this;
			}
		}

		void UninitControllable(LivingObject living)
		{
			living.Destructed -= OnControllableDestructed;
			living.Controller = null;
		}

		void InitControllablesVisionTracker(IEnumerable<LivingObject> controllables)
		{
			Debug.Assert(this.IsConnected);

			foreach (var c in controllables)
			{
				c.ParentChanged += OnControllableParentChanged;

				if (c.Environment != null)
				{
					var tracker = GetVisionTrackerInternal(c.Environment);
					tracker.AddLiving(c);
				}
			}
		}

		void UninitControllableVisionTracker(LivingObject living)
		{
			living.ParentChanged -= OnControllableParentChanged;

			if (living.Environment != null)
			{
				var tracker = GetVisionTrackerInternal(living.Environment);
				tracker.RemoveLiving(living);
			}
		}

		void SendAddControllables(IEnumerable<LivingObject> controllables)
		{
			Debug.Assert(this.IsConnected);

			// Always send object data when the living has became a controllable
			foreach (var c in controllables)
				c.SendTo(this, ObjectVisibility.All);

			Send(new Messages.ControllablesDataMessage()
			{
				Operation = ControllablesDataMessage.Op.Add,
				Controllables = controllables.Select(c => c.ObjectID).ToArray(),
			});
		}

		void SendRemoveControllable(LivingObject living)
		{
			Debug.Assert(this.IsConnected);

			Send(new Messages.ControllablesDataMessage()
			{
				Operation = ControllablesDataMessage.Op.Remove,
				Controllables = new ObjectID[] { living.ObjectID }
			});
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

		public void HandleNewMessages()
		{
			trace.TraceVerbose("HandleNewMessages");

			Message msg;
			while (m_connection.TryGetMessage(out msg))
				OnReceiveMessage(msg);

			if (!m_connection.IsConnected)
			{
				trace.TraceInformation("HandleNewMessages, disconnected");

				OnDisconnected();
			}
		}

		void OnReceiveMessage(Message m)
		{
			trace.TraceVerbose("OnReceiveMessage({0})", m);

			var msg = (ServerMessage)m;

			Action<Player, ServerMessage> method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void ReceiveMessage(LogOutRequestMessage msg)
		{
			Send(new Messages.LogOutReplyMessage());

			m_connection.Disconnect();
		}

		void ReceiveMessage(SetWorldConfigMessage msg)
		{
			if (msg.MinTickTime.HasValue)
				m_engine.SetMinTickTime(msg.MinTickTime.Value);
		}

		/* functions for livings */
		void ReceiveMessage(ProceedTurnReplyMessage msg)
		{
			try
			{
				if (this.IsProceedTurnReplyReceived == true)
					throw new Exception();

				foreach (var tuple in msg.Actions)
				{
					var actorOid = tuple.Item1;
					var action = tuple.Item2;

					if (m_world.CurrentLivingID != ObjectID.AnyObjectID && m_world.CurrentLivingID != actorOid)
					{
						trace.TraceWarning("received action request for living who's turn is not now: {0}", actorOid);
						continue;
					}

					var living = m_controllables.SingleOrDefault(l => l.ObjectID == actorOid);

					if (living == null)
					{
						trace.TraceWarning("received action request for non controlled living {0}", actorOid);
						continue;
					}

					if (living.Controller != this)
						throw new Exception();

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

			if (change is TurnEndChange)
				this.IsProceedTurnReplyReceived = false;
		}

		Dictionary<EnvironmentObject, VisionTrackerBase> m_visionTrackers = new Dictionary<EnvironmentObject, VisionTrackerBase>();

		public ObjectVisibility GetObjectVisibility(BaseObject ob)
		{
			switch (ob.ObjectType)
			{
				case ObjectType.Item:
				case ObjectType.Living:
					{
						var mo = (MovableObject)ob;

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

				case ObjectType.Building:
				case ObjectType.Environment:
					return ObjectVisibility.All;

				default:
					throw new Exception();
			}
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
						tracker = new VisionTrackerGlobalFOV(this, env);
						break;

					case VisibilityMode.LivingLOS:
						tracker = new VisionTrackerLOS(this, env);
						break;

					default:
						throw new NotImplementedException();
				}

				m_visionTrackers[env] = tracker;
			}

			return tracker;
		}

		void OnControllableParentChanged(LivingObject living, ContainerObject _src, ContainerObject _dst)
		{
			var src = _src as EnvironmentObject;

			if (src != null)
			{
				var tracker = GetVisionTrackerInternal(src);
				tracker.RemoveLiving(living);
			}

			var dst = _dst as EnvironmentObject;

			if (dst != null)
			{
				var tracker = GetVisionTrackerInternal(src);
				tracker.AddLiving(living);
			}
		}
	}
}
