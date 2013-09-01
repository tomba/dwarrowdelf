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
	public sealed class Player : IPlayer
	{
		/// <summary>
		/// UserID of the user who owns this Player
		/// </summary>
		[SaveGameProperty]
		public int UserID { get; private set; }

		/// <summary>
		/// User who is currently connected to this Player
		/// </summary>
		public User User { get; private set; }

		[SaveGameProperty]
		public int PlayerID { get; private set; }

		// does this player see all
		[SaveGameProperty("SeeAll")]
		bool m_seeAll;

		[SaveGameProperty]
		GameEngine m_engine;

		[SaveGameProperty]
		World m_world;

		[SaveGameProperty("Controllables")]
		List<LivingObject> m_controllables;

		public bool IsConnected { get { return this.User != null; } }

		public bool IsController(BaseObject living) { return m_controllables.Contains(living); }

		ChangeHandler m_changeHandler;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection");

		public bool IsProceedTurnReplyReceived { get; private set; }
		public event Action<Player> ProceedTurnReceived;

		public event Action<Player> DisconnectEvent;

		public Player(int playerID, GameEngine engine)
		{
			// player ID 0 is invalid, 1 is reserved for server
			if (playerID == 0 || playerID == 1)
				throw new Exception();

			this.PlayerID = playerID;

			m_engine = engine;
			m_world = engine.World;

			m_seeAll = false;

			m_controllables = new List<LivingObject>();

			Construct();
		}

		Player(SaveGameContext ctx)
		{
			InitControllables(m_controllables);

			Construct();
		}

		public override string ToString()
		{
			return String.Format("Player({0})", this.PlayerID);
		}

		void Construct()
		{
			trace.Header = String.Format("Player({0})", this.PlayerID);

			if (m_seeAll)
				m_changeHandler = new AdminChangeHandler(this);
			else
				m_changeHandler = new PlayerChangeHandler(this);
		}

		// Called from User
		internal void ConnectUser(User user)
		{
			if (this.User != null)
				throw new Exception();

			this.User = user;
			this.UserID = user.UserID;

			OnConnected();
		}

		// Called from User
		internal void DisconnectUser()
		{
			if (this.User == null)
				throw new Exception();

			this.User = null;

			OnDisconnected();
		}

		void OnConnected()
		{
			trace.TraceInformation("OnConnected");

			m_world.WorldChanged += HandleWorldChange;
			m_world.ReportReceived += HandleReport;

			Send(new Messages.LogOnReplyBeginMessage()
			{
				PlayerID = this.PlayerID,
				IsSeeAll = m_seeAll,
			});

			m_world.SendWorldData(this);

			m_world.SendTo(this, m_seeAll ? ObjectVisibility.All : ObjectVisibility.Public);

			InitControllablesVisionTracker(m_controllables);
			SendAddControllables(m_controllables);

			Send(new Messages.ClientDataMessage()
			{
				ClientData = m_engine.LoadClientData(this.PlayerID, m_engine.LastLoadID),
			});

			Send(new Messages.LogOnReplyEndMessage()
			{
			});

			if (m_world.IsTickOnGoing)
			{
				if (m_world.TickMethod == WorldTickMethod.Simultaneous)
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

			foreach (var c in m_controllables)
				UninitControllableVisionTracker(c);

			m_world.WorldChanged -= HandleWorldChange;
			m_world.ReportReceived -= HandleReport;

			this.IsProceedTurnReplyReceived = false;

			if (DisconnectEvent != null)
				DisconnectEvent(this);
		}

		public void AddControllables(IEnumerable<LivingObject> controllables)
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
			if (this.IsConnected)
				this.User.Send(msg);
		}

		public void Send(IEnumerable<ClientMessage> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}

		// Called from User
		public void DispatchMessage(Message m)
		{
			trace.TraceVerbose("DispatchMessage({0})", m);

			var msg = (ServerMessage)m;

			Action<Player, ServerMessage> method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		/* functions for livings */
		void ReceiveMessage(ProceedTurnReplyMessage msg)
		{
			if (this.IsProceedTurnReplyReceived == true)
				throw new Exception();

			foreach (var kvp in msg.Actions)
			{
				var actorOid = kvp.Key;
				var action = kvp.Value;

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

				living.StartAction(action, ActionPriority.User);
			}

			this.IsProceedTurnReplyReceived = true;

			if (ProceedTurnReceived != null)
				ProceedTurnReceived(this);
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
			if (m_seeAll)
				return ObjectVisibility.All;

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
	}
}
