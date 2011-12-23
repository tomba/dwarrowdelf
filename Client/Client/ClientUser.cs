using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	sealed class ClientUser
	{
		static Dictionary<Type, Action<ClientUser, ClientMessage>> s_handlerMap;

		static ClientUser()
		{
			var messageTypes = Helpers.GetNonabstractSubclasses(typeof(ClientMessage));

			s_handlerMap = new Dictionary<Type, Action<ClientUser, ClientMessage>>(messageTypes.Count());

			foreach (var type in messageTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ClientUser, ClientMessage>("HandleMessage", type);
				if (method != null)
					s_handlerMap[type] = method;
			}
		}

		ReportHandler m_reportHandler;
		ChangeHandler m_changeHandler;

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

			m_reportHandler = new ReportHandler(m_world);
			m_changeHandler = new ChangeHandler(m_world);
			m_changeHandler.TurnEnded += OnTurnEnded;
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
				var l = m_world.FindOrCreateObject<LivingObject>(oid);
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
			var ob = m_world.FindOrCreateObject<BaseObject>(data.ObjectID);
			ob.Deserialize(data);
		}

		void HandleMessage(MapDataTerrainsMessage msg)
		{
			var env = m_world.GetObject<EnvironmentObject>(msg.Environment);
			env.SetTerrains(msg.Bounds, msg.TerrainData);
		}

		void HandleMessage(MapDataTerrainsListMessage msg)
		{
			var env = m_world.GetObject<EnvironmentObject>(msg.Environment);
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

		void HandleMessage(ReportMessage msg)
		{
			m_reportHandler.HandleReportMessage(msg);
		}

		void HandleMessage(ChangeMessage msg)
		{
			m_changeHandler.HandleChangeMessage(msg);
		}


		// Called from change handler
		void OnTurnEnded()
		{
			m_turnActionRequested = false;
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
				var living = m_world.GetObject<LivingObject>(livingID);
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
	}
}
