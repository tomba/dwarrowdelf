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
				var l = m_world.GetObject<LivingObject>(oid);
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
			var ob = m_world.GetObject<BaseObject>(data.ObjectID);
			ob.Deserialize(data);
		}

		void HandleMessage(MapDataTerrainsMessage msg)
		{
			var env = m_world.FindObject<EnvironmentObject>(msg.Environment);
			if (env == null)
				throw new Exception();
			env.SetTerrains(msg.Bounds, msg.TerrainData);
		}

		void HandleMessage(MapDataTerrainsListMessage msg)
		{
			var env = m_world.FindObject<EnvironmentObject>(msg.Environment);
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
			var ob = m_world.FindObject<BaseObject>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.Deserialize(change.ObjectData);
		}

		void HandleChange(ObjectMoveChange change)
		{
			var ob = m_world.FindObject<MovableObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			Debug.Assert(ob.IsInitialized);

			ContainerObject env = null;
			if (change.DestinationID != ObjectID.NullObjectID)
				env = m_world.FindObject<ContainerObject>(change.DestinationID);

			ob.MoveTo(env, change.DestinationLocation);
		}

		void HandleChange(ObjectMoveLocationChange change)
		{
			var ob = m_world.FindObject<MovableObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			Debug.Assert(ob.IsInitialized);

			ob.MoveTo(change.DestinationLocation);
		}

		void HandleChange(DamageChange change)
		{
			var attacker = m_world.FindObject<LivingObject>(change.AttackerID);
			var target = m_world.GetObject<LivingObject>(change.ObjectID);

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
			var target = m_world.GetObject<LivingObject>(change.ObjectID);

			GameData.Data.AddGameEvent(target, "{0} dies", target);
		}

		void HandleChange(PropertyObjectChange change)
		{
			var ob = m_world.FindObject<BaseObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(PropertyIntChange change)
		{
			var ob = m_world.FindObject<BaseObject>(change.ObjectID);

			if (ob == null)
			{
				trace.TraceWarning("Unknown object {0} for propertychange {1}", change.ObjectID, change.PropertyID);
				return;
			}

			Debug.Assert(ob.IsInitialized);

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(SkillChange change)
		{
			var ob = m_world.FindObject<LivingObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			ob.SetSkillLevel(change.SkillID, change.Level);
		}

		void HandleChange(WearChange change)
		{
			var ob = m_world.FindObject<LivingObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			if (change.WearableID != ObjectID.NullObjectID)
			{
				var wearable = m_world.GetObject<ItemObject>(change.WearableID);
				ob.WearArmor(change.Slot, wearable);
				GameData.Data.AddGameEvent(ob, "{0} wears {1}", ob, wearable);
			}
			else
			{
				ob.RemoveArmor(change.Slot);
				GameData.Data.AddGameEvent(ob, "{0} removes armor from slot {1}", ob, change.Slot);
			}
		}


		void HandleChange(WieldChange change)
		{
			var ob = m_world.FindObject<LivingObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			if (change.WeaponID != ObjectID.NullObjectID)
			{
				var weapon = m_world.GetObject<ItemObject>(change.WeaponID);
				ob.WieldWeapon(weapon);
				GameData.Data.AddGameEvent(ob, "{0} wields {1}", ob, weapon);
			}
			else
			{
				ob.RemoveWeapon();
				GameData.Data.AddGameEvent(ob, "{0} removes weapon", ob);
			}
		}

		void HandleChange(ObjectDestructedChange change)
		{
			var ob = m_world.FindObject<BaseObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.Destruct();
		}

		void HandleChange(MapChange change)
		{
			var env = m_world.FindObject<EnvironmentObject>(change.EnvironmentID);
			if (env == null)
				throw new Exception();

			Debug.Assert(env.IsInitialized);

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
				var living = m_world.FindObject<LivingObject>(livingID);
				if (living == null)
					throw new Exception();
				m_activeLiving = living;
			}
		}

		bool m_turnActionRequested;
		LivingObject m_activeLiving;
		Dictionary<LivingObject, GameAction> m_actionMap = new Dictionary<LivingObject, GameAction>();

		public void SignalLivingHasAction(LivingObject living, GameAction action)
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
					action = living.DecideAction();

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

			var ob = m_world.FindObject<LivingObject>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionStarted(change);
		}

		void HandleChange(ActionProgressChange change)
		{
			var ob = m_world.FindObject<LivingObject>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionProgress(change);
		}

		void HandleChange(ActionDoneChange change)
		{
			var ob = m_world.FindObject<LivingObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionDone(change);
		}
	}
}
