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
		Dictionary<Type, Action<ServerMessage>> m_handlerMap = new Dictionary<Type, Action<ServerMessage>>();
		Dictionary<Type, Action<Change>> m_changeHandlerMap = new Dictionary<Type, Action<Change>>();

		public bool IsCharConnected { get; private set; }

		ClientConnection m_connection;

		public int UserID { get; private set; }
		public bool IsSeeAll { get; private set; }

		World m_world;

		Action m_enterGameCallback;

		public event Action ExitedGameEvent;

		public ClientUser(ClientConnection connection, World world, int userID, bool isSeeAll)
		{
			m_connection = connection;
			m_world = world;
			this.UserID = userID;
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

		public void OnReceiveMessage(ServerMessage msg)
		{
			//Debug.Print("Received Message {0}", msg);

			Action<ServerMessage> f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<ServerMessage>("HandleMessage", t, this);

				if (f == null)
					throw new Exception(String.Format("No msg handler for {0}", msg.GetType()));

				m_handlerMap[t] = f;
			}

			f(msg);
		}

		void HandleMessage(EnterGameReplyMessage msg)
		{
			Debug.Assert(!this.IsCharConnected);

			this.IsCharConnected = true;

			m_enterGameCallback();
		}

		void HandleMessage(ControllablesDataMessage msg)
		{
			GameData.Data.World.Controllables.Clear();

			foreach (var oid in msg.Controllables)
			{
				var l = GameData.Data.World.FindObject<Living>(oid);
				if (l == null)
					l = new Living(GameData.Data.World, oid);
				GameData.Data.World.Controllables.Add(l);
			}
		}

		void HandleMessage(ExitGameReplyMessage msg)
		{
			Debug.Assert(this.IsCharConnected);

			this.IsCharConnected = false;

			if (ExitedGameEvent != null)
				ExitedGameEvent();

			GameData.Data.World.Controllables.Clear();
			GameData.Data.CurrentObject = null;
			//App.MainWindow.FollowObject = null;
		}


		void HandleMessage(ObjectDataMessage msg)
		{
			HandleObjectData(msg.ObjectData);
		}

		void HandleMessage(ObjectDataArrayMessage msg)
		{
			foreach (var data in msg.ObjectDatas)
				HandleObjectData(data);
		}

		void HandleObjectData(BaseGameObjectData data)
		{
			var ob = GameData.Data.World.FindObject<BaseGameObject>(data.ObjectID);

			if (ob == null)
			{
				Debug.Print("New object {0} of type {1} appeared", data.ObjectID, data.GetType().Name);

				if (data is LivingData)
					ob = new Living(GameData.Data.World, data.ObjectID);
				else if (data is ItemData)
					ob = new ItemObject(GameData.Data.World, data.ObjectID);
				else if (data is BuildingData)
					ob = new BuildingObject(GameData.Data.World, data.ObjectID);
				else
					throw new Exception();
			}

			ob.Deserialize(data);
		}

		DateTime m_mapDataStartTime;
		void HandleMessage(MapDataStart msg)
		{
			m_mapDataStartTime = DateTime.Now;
			Trace.TraceInformation("Map transfer start");
		}

		void HandleMessage(MapDataEnd msg)
		{
			var time = DateTime.Now - m_mapDataStartTime;

			Trace.TraceInformation("Map transfer took {0}", time);
		}

		void HandleMessage(MapDataMessage msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);

			if (env == null)
			{
				Debug.Print("New map appeared {0}", msg.Environment);
				var world = GameData.Data.World;
				if (msg.Bounds.IsNull)
					env = new Environment(world, msg.Environment, msg.HomeLocation);
				else
					env = new Environment(world, msg.Environment, msg.Bounds, msg.HomeLocation);
			}

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
			Debug.Print("Received TerrainData for {0} tiles", msg.TileDataList.Count());
			env.SetTerrains(msg.TileDataList);
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

			switch (change.ObjectType)
			{
				case ObjectCreatedChange.ObjectTypes.Environment:
					new Environment(world, change.ObjectID, new IntPoint3D());
					break;

				case ObjectCreatedChange.ObjectTypes.Living:
					new Living(world, change.ObjectID);
					break;

				case ObjectCreatedChange.ObjectTypes.Item:
					new ItemObject(world, change.ObjectID);
					break;

				case ObjectCreatedChange.ObjectTypes.Building:
					new BuildingObject(world, change.ObjectID);
					break;

				default:
					throw new Exception();
			}
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

		void HandleChange(PropertyChange change)
		{
			var ob = GameData.Data.World.FindObject<ClientGameObject>(change.ObjectID);

			if (ob == null)
				throw new Exception();

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

		void HandleChange(TurnStartChange change)
		{
			Debug.Assert(m_turnActionRequested == false);

			m_turnActionRequested = true;

			if (change.LivingID == ObjectID.NullObjectID)
			{
				if (GameData.Data.IsAutoAdvanceTurn)
					SendProceedTurn();
			}
			else
			{
				var living = GameData.Data.World.FindObject<Living>(change.LivingID);
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

			m_connection.Send(new ProceedTurnMessage() { Actions = list.ToArray() });

			m_activeLiving = null;
			m_actionMap.Clear();
		}

		void HandleChange(TurnEndChange change)
		{
			m_turnActionRequested = false;
		}

		void HandleChange(ActionStartedChange change)
		{
			var ob = GameData.Data.World.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionStarted(change);
		}

		void HandleChange(ActionProgressChange change)
		{
			//MyDebug.WriteLine("ActionProgressChange({0})", e.TransactionID);

			var ob = GameData.Data.World.FindObject<Living>(change.ObjectID);
			if (ob == null)
				throw new Exception();

			ob.HandleActionProgress(change);
		}
	}
}
