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
		Dictionary<Type, Action<ClientMessage>> m_handlerMap = new Dictionary<Type, Action<ClientMessage>>();
		Dictionary<Type, Action<Change>> m_changeHandlerMap = new Dictionary<Type, Action<Change>>();

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

			Action<ClientMessage> f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<ClientMessage>("HandleMessage", t, this);

				if (f == null)
					throw new Exception(String.Format("No msg handler for {0}", msg.GetType()));

				m_handlerMap[t] = f;
			}

			f(msg);
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
			GameData.Data.World.Controllables.Clear();

			foreach (var oid in msg.Controllables)
			{
				var l = GameData.Data.World.GetObject<Living>(oid);
				l.IsControllable = true;
			}
		}

		void HandleMessage(ExitGameReplyMessage msg)
		{
			Debug.Assert(this.IsPlayerInGame);

			this.IsPlayerInGame = false;

			if (ExitedGameEvent != null)
				ExitedGameEvent();

			GameData.Data.World.Controllables.Clear();
			GameData.Data.CurrentObject = null;
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
			var ob = GameData.Data.World.GetObject<BaseGameObject>(data.ObjectID);
			ob.Deserialize(data);
		}

		void HandleMessage(MapDataMessage msg)
		{
			var env = GameData.Data.World.GetObject<Environment>(msg.Environment);

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
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			env.SetTerrains(msg.Bounds, msg.TerrainData);
		}

		void HandleMessage(MapDataTerrainsListMessage msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
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
			App.MainWindow.outputTextBox.AppendText(msg.Text);
			App.MainWindow.outputTextBox.ScrollToEnd();
		}

		void HandleMessage(ChangeMessage msg)
		{
			var change = msg.Change;

			Action<Change> f;
			Type t = change.GetType();
			if (!m_changeHandlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<Change>("HandleChange", t, this);

				if (f == null)
					throw new Exception(String.Format("No change handler for {0}", change.GetType()));

				m_changeHandlerMap[t] = f;
			}

			//MyDebug.WriteLine("Change: {0}", msg);

			f(change);
		}

		void HandleChange(ObjectCreatedChange change)
		{
			var world = GameData.Data.World;
			// just create the object
			var ob = world.GetObject(change.ObjectID);
		}

		// XXX check if this is needed
		void HandleChange(FullObjectChange change)
		{
			var ob = GameData.Data.World.FindObject<BaseGameObject>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.Deserialize(change.ObjectData);
		}

		void HandleChange(ObjectMoveChange change)
		{
			ClientGameObject ob = GameData.Data.World.FindObject<ClientGameObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			ClientGameObject env = null;
			if (change.DestinationMapID != ObjectID.NullObjectID)
				env = GameData.Data.World.FindObject<ClientGameObject>(change.DestinationMapID);

			ob.MoveTo(env, change.DestinationLocation);
		}

		void HandleChange(PropertyObjectChange change)
		{
			var ob = GameData.Data.World.FindObject<ClientGameObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(PropertyIntChange change)
		{
			var ob = GameData.Data.World.FindObject<ClientGameObject>(change.ObjectID);

			if (ob == null)
			{
				trace.TraceWarning("Unknown object {0} for propertychange {1}", change.ObjectID, change.PropertyID);
				return;
			}

			ob.SetProperty(change.PropertyID, change.Value);
		}

		void HandleChange(ObjectDestructedChange change)
		{
			var ob = GameData.Data.World.FindObject<ClientGameObject>(change.ObjectID);

			ob.Destruct();
		}

		void HandleChange(MapChange change)
		{
			var env = GameData.Data.World.FindObject<Environment>(change.EnvironmentID);
			if (env == null)
				throw new Exception();
			env.SetTileData(change.Location, change.TileData);
		}

		void HandleChange(TickStartChange change)
		{
			GameData.Data.World.HandleChange(change);
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
				var living = GameData.Data.World.FindObject<Living>(livingID);
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
			var livings = GameData.Data.World.Controllables.Where(l => l.UserActionPossible());
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

			var ob = GameData.Data.World.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionStarted(change);
		}

		void HandleChange(ActionProgressChange change)
		{
			//Debug.WriteLine("ActionProgressChange({0})", change.ObjectID);

			var ob = GameData.Data.World.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionProgress(change);
		}
	}
}
