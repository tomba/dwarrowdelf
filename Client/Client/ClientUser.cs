using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class ClientUser
	{
		static Dictionary<Type, Action<ClientUser, ClientMessage>> s_handlerMap;
		static Dictionary<Type, Action<ClientUser, Change>> s_changeHandlerMap;

		static ClientUser()
		{
			var changeTypes = Helpers.GetNonabstractSubclasses(typeof(Change));

			s_changeHandlerMap = new Dictionary<Type, Action<ClientUser, Change>>(changeTypes.Count());

			foreach (var type in changeTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ClientUser, Change>("HandleChange", type);
				if (method != null)
					s_changeHandlerMap[type] = method;
			}

			var messageTypes = Helpers.GetNonabstractSubclasses(typeof(ClientMessage));

			s_handlerMap = new Dictionary<Type, Action<ClientUser, ClientMessage>>(messageTypes.Count());

			foreach (var type in messageTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ClientUser, ClientMessage>("HandleMessage", type);
				if (method != null)
					s_handlerMap[type] = method;
			}
		}

		ClientConnection m_connection;

		public bool IsSeeAll { get; private set; }
		public bool IsPlayerInGame { get; private set; }

		World m_world;

		Action m_enterGameCallback;

		public event Action ExitedGameEvent;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection", "ClientUser");

		public ClientUser(ClientConnection connection, World world, bool isSeeAll)
		{
			m_connection = connection;
			m_world = world;
			this.IsSeeAll = isSeeAll;
		}

		public void SendEnterGame(Action callback)
		{
			m_enterGameCallback = callback;
			m_connection.Send(new Messages.EnterGameRequestMessage() { Name = "tomba" });
		}

		public void SendExitGame()
		{
			m_connection.Send(new Messages.ExitGameRequestMessage());
		}

		public void OnReceiveMessage(ClientMessage msg)
		{
			//trace.TraceVerbose("Received Message {0}", msg);

			var method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void HandleMessage(EnterGameReplyBeginMessage msg)
		{
			trace.TraceInformation("EnterGameReplyBeginMessage");

			Debug.Assert(!this.IsPlayerInGame);
		}

		void HandleMessage(EnterGameReplyEndMessage msg)
		{
			trace.TraceInformation("EnterGameReplyEndMessage");

			this.IsPlayerInGame = true;

			if (msg.ClientData != null)
				ClientSaveManager.Load(msg.ClientData);

			m_enterGameCallback();
		}

		void HandleMessage(ControllablesDataMessage msg)
		{
			bool b;

			switch (msg.Operation)
			{
				case ControllablesDataMessage.Op.Add:
					b = true;
					break;

				case ControllablesDataMessage.Op.Remove:
					b = false;
					break;

				default:
					throw new Exception();
			}

			foreach (var oid in msg.Controllables)
			{
				var l = m_world.GetObject<Living>(oid);
				l.IsControllable = b;
			}
		}

		void HandleMessage(ExitGameReplyMessage msg)
		{
			Debug.Assert(this.IsPlayerInGame);

			this.IsPlayerInGame = false;

			if (ExitedGameEvent != null)
				ExitedGameEvent();

			m_world.Controllables.Clear();
			//App.MainWindow.FollowObject = null;
		}

		void HandleMessage(SaveClientDataRequestMessage msg)
		{
			ClientSaveManager.Save(msg.ID);
		}

		void HandleMessage(ObjectDataMessage msg)
		{
			HandleObjectData(msg.ObjectData);
		}

		void HandleObjectData(BaseGameObjectData data)
		{
			var ob = m_world.GetObject<BaseGameObject>(data.ObjectID);
			ob.Deserialize(data);
		}

		void HandleMessage(MapDataMessage msg)
		{
			var env = m_world.GetObject<Environment>(msg.Environment);

			if (!msg.Bounds.IsNull)
				env.Bounds = msg.Bounds;
			env.HomeLocation = msg.HomeLocation;
			env.VisibilityMode = msg.VisibilityMode;

			// XXX
			if (App.MainWindow.map.Environment == null)
				App.MainWindow.map.Environment = env;
		}

		void HandleMessage(MapDataTerrainsMessage msg)
		{
			var env = m_world.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			env.SetTerrains(msg.Bounds, msg.TerrainData);
		}

		void HandleMessage(MapDataTerrainsListMessage msg)
		{
			var env = m_world.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			trace.TraceVerbose("Received TerrainData for {0} tiles", msg.TileDataList.Count());
			env.SetTerrains(msg.TileDataList);
		}

		void HandleMessage(ProceedTurnRequestMessage msg)
		{
			TurnActionRequested(msg.LivingID);
		}

		void HandleMessage(IPOutputMessage msg)
		{
			GameData.Data.AddIPMessage(msg);
		}

		void HandleMessage(ChangeMessage msg)
		{
			var change = msg.Change;

			var method = s_changeHandlerMap[change.GetType()];
			method(this, change);
		}

		void HandleChange(ObjectCreatedChange change)
		{
			// just create the object
			var ob = m_world.GetObject(change.ObjectID);
		}

		// XXX check if this is needed
		void HandleChange(FullObjectChange change)
		{
			var ob = m_world.FindObject<BaseGameObject>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.Deserialize(change.ObjectData);
		}

		void HandleChange(ObjectMoveChange change)
		{
			var ob = m_world.FindObject<GameObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			GameObject env = null;
			if (change.DestinationMapID != ObjectID.NullObjectID)
				env = m_world.FindObject<GameObject>(change.DestinationMapID);

			ob.MoveTo(env, change.DestinationLocation);
		}

		void HandleChange(ObjectMoveLocationChange change)
		{
			var ob = m_world.FindObject<GameObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			ob.MoveTo(change.DestinationLocation);
		}

		void HandleChange(DamageChange change)
		{
			var attacker = m_world.FindObject<Living>(change.AttackerID);
			var target = m_world.GetObject<Living>(change.ObjectID);

			string aname = attacker == null ? "nobody" : attacker.ToString();
			string tname = target.ToString();

			string msg;

			if (change.IsHit)
			{
				switch (change.DamageCategory)
				{
					case DamageCategory.None:
						msg = String.Format("{0} hits {1}, dealing {2} damage", aname, tname, change.Damage);
						break;

					case DamageCategory.Melee:
						msg = String.Format("{0} hits {1}, dealing {2} damage", aname, tname, change.Damage);
						break;

					default:
						throw new Exception();
				}
			}
			else
			{
				msg = String.Format("{0} misses {1}", aname, tname);
			}

			GameData.Data.AddGameEvent(attacker, msg);
		}

		void HandleChange(DeathChange change)
		{
			var target = m_world.GetObject<Living>(change.ObjectID);

			GameData.Data.AddGameEvent(target, "{0} dies", target);
		}

		void HandleChange(PropertyObjectChange change)
		{
			var ob = m_world.FindObject<BaseGameObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(PropertyIntChange change)
		{
			var ob = m_world.FindObject<BaseGameObject>(change.ObjectID);

			if (ob == null)
			{
				trace.TraceWarning("Unknown object {0} for propertychange {1}", change.ObjectID, change.PropertyID);
				return;
			}

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(SkillChange change)
		{
			var ob = m_world.FindObject<Living>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			ob.SetSkillLevel(change.SkillID, change.Level);
		}

		void HandleChange(ObjectDestructedChange change)
		{
			var ob = m_world.FindObject<BaseGameObject>(change.ObjectID);

			ob.Destruct();
		}

		void HandleChange(MapChange change)
		{
			var env = m_world.FindObject<Environment>(change.EnvironmentID);
			if (env == null)
				throw new Exception();
			env.SetTileData(change.Location, change.TileData);
		}

		void HandleChange(TickStartChange change)
		{
			m_world.HandleChange(change);
		}

		void HandleChange(TurnStartSimultaneousChange change)
		{
		}

		void HandleChange(TurnStartSequentialChange change)
		{
		}

		void TurnActionRequested(ObjectID livingID)
		{
			trace.TraceVerbose("Turn Action requested for living: {0}", livingID);

			Debug.Assert(m_turnActionRequested == false);

			m_turnActionRequested = true;

			if (livingID == ObjectID.NullObjectID)
			{
				throw new Exception();
			}
			else if (livingID == ObjectID.AnyObjectID)
			{
				if (GameData.Data.IsAutoAdvanceTurn)
					SendProceedTurn();
			}
			else
			{
				var living = m_world.FindObject<Living>(livingID);
				if (living == null)
					throw new Exception();
				m_activeLiving = living;
			}
		}

		bool m_turnActionRequested;
		Living m_activeLiving;
		Dictionary<Living, GameAction> m_actionMap = new Dictionary<Living, GameAction>();

		public void SignalLivingHasAction(Living living, GameAction action)
		{
			if (m_turnActionRequested == false)
				return;

			if (m_activeLiving == null)
				throw new Exception();

			if (m_activeLiving != living)
				throw new Exception();

			m_actionMap[living] = action;

			if (GameData.Data.IsAutoAdvanceTurn)
				SendProceedTurn();
		}

		public void SendProceedTurn()
		{
			if (m_turnActionRequested == false)
				return;

			// livings which the user can control (ie. server not doing high priority action)
			var livings = m_world.Controllables.Where(l => l.UserActionPossible());
			var list = new List<Tuple<ObjectID, GameAction>>();
			foreach (var living in livings)
			{
				GameAction action;

				if (m_actionMap.ContainsKey(living))
					action = m_actionMap[living];
				else
					action = living.DecideAction(ActionPriority.Normal);

				if (action != living.CurrentAction)
					list.Add(new Tuple<ObjectID, GameAction>(living.ObjectID, action));
			}

			m_connection.Send(new ProceedTurnReplyMessage() { Actions = list.ToArray() });

			m_activeLiving = null;
			m_actionMap.Clear();
		}

		void HandleChange(TurnEndSimultaneousChange change)
		{
			m_turnActionRequested = false;
		}

		void HandleChange(TurnEndSequentialChange change)
		{
			m_turnActionRequested = false;
		}

		void HandleChange(ActionStartedChange change)
		{
			//Debug.WriteLine("ActionStartedChange({0})", change.ObjectID);

			var ob = m_world.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionStarted(change);
		}

		void HandleChange(ActionProgressChange change)
		{
			var ob = m_world.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionProgress(change);
		}

		void HandleChange(ActionDoneChange change)
		{
			var ob = m_world.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionDone(change);
		}
	}
}
