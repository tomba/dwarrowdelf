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
			{
				l.Destructed += OnControllableDestructed; // XXX remove if player deleted
				l.Controller = this;
			}

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
			Debug.Assert(m_connection == null);

			m_world.WorldChanged += HandleWorldChange;
			m_world.ReportReceived += HandleReport;

			m_connection = connection;

			Send(new Messages.LogOnReplyBeginMessage()
			{
				IsSeeAll = this.IsSeeAll,
				Tick = m_world.TickNumber,
				LivingVisionMode = m_world.LivingVisionMode,
				GameMode = m_world.GameMode,
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
				{
					this.IsProceedTurnRequestSent = true;

					var change = new TurnStartChange(null);
					var changeMsg = new ChangeMessage() { ChangeData = change.ToChangeData() };
					Send(changeMsg);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
		}

		void HandleDisconnect()
		{
			trace.TraceInformation("HandleDisconnect");

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

		public void HandleNewMessages()
		{
			trace.TraceVerbose("HandleNewMessages");

			Message msg;
			while (m_connection.TryGetMessage(out msg))
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
			living.Controller = this;

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
			living.Controller = null;
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
			{
				if (living.Environment != null)
				{
					var tracker = GetVisionTrackerInternal(living.Environment);
					tracker.HandleNewControllable(living);
				}
			}

			foreach (var living in this.Controllables)
				living.SendTo(this, ObjectVisibility.All);

			Send(new Messages.ControllablesDataMessage()
			{
				Operation = ControllablesDataMessage.Op.Add,
				Controllables = this.Controllables.Select(l => l.ObjectID).ToArray()
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

			if (change is TurnStartChange)
			{
				var c = (TurnStartChange)change;
				if (c.Living == null || this.IsController(c.Living))
					this.IsProceedTurnRequestSent = true;
			}
			else if (change is TurnEndChange)
			{
				this.IsProceedTurnRequestSent = false;
				this.IsProceedTurnReplyReceived = false;
			}
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
						tracker = new VisionTrackerGlobalFOV(this, env);
						break;

					case VisibilityMode.LivingLOS:
						tracker = new VisionTrackerLOS(this, env);
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
}
