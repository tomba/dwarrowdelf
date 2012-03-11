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

		public event Action DisconnectEvent;

		ReportHandler m_reportHandler;
		ChangeHandler m_changeHandler;

		Connection m_connection;

		public bool IsSeeAll { get; private set; }
		public bool IsPlayerInGame { get; private set; }

		World m_world;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection", "ClientUser");

		public ClientNetStatistics Stats { get; private set; }

		public ClientUser()
		{
			this.Stats = new ClientNetStatistics();
		}

		public void LogOnSync(string name, Action<string> callback)
		{
			try
			{
				m_connection = Connection.Connect();

				m_connection.Send(new Messages.LogOnRequestMessage() { Name = name });

				var msg = m_connection.Receive();

				var reply = msg as Messages.LogOnReplyBeginMessage;

				if (reply == null)
					throw new Exception();

				m_world = new World();
				GameData.Data.World = m_world;

				m_reportHandler = new ReportHandler(m_world);
				m_changeHandler = new ChangeHandler(m_world);
				m_changeHandler.TurnEnded += OnTurnEnded;

				m_world.SetLivingVisionMode(reply.LivingVisionMode);
				m_world.SetTick(reply.Tick);
				this.IsSeeAll = reply.IsSeeAll;

				callback(null);

				m_connection.Start(_OnReceiveMessage, _OnDisconnected);
			}
			catch (Exception e)
			{
				callback(e.Message);
			}
		}

		public void Send(ServerMessage msg)
		{
			if (m_connection == null)
			{
				trace.TraceWarning("Send: m_connection == null");
				return;
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceWarning("Send: m_connection.IsConnected == false");
				return;
			}

			m_connection.Send(msg);

			this.Stats.SentBytes = m_connection.SentBytes;
			this.Stats.SentMessages = m_connection.SentMessages;
			this.Stats.AddSentMessages(msg);
		}

		public void SendLogOut()
		{
			m_connection.Send(new Messages.LogOutRequestMessage());
		}

		void _OnDisconnected()
		{
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(OnDisconnected));
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnect");

			if (DisconnectEvent != null)
				DisconnectEvent();
		}

		void _OnReceiveMessage(Message msg)
		{
			System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action<ClientMessage>(OnReceiveMessage), msg);
		}
		public void OnReceiveMessage(ClientMessage msg)
		{
			//trace.TraceVerbose("Received Message {0}", msg);

			this.Stats.ReceivedBytes = m_connection.ReceivedBytes;
			this.Stats.ReceivedMessages = m_connection.ReceivedMessages;
			this.Stats.AddReceivedMessages(msg);

			var method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void HandleMessage(LogOnReplyEndMessage msg)
		{
			if (msg.ClientData != null)
				ClientSaveManager.Load(msg.ClientData);

			var controllable = GameData.Data.World.Controllables.FirstOrDefault();
			if (controllable != null && controllable.Environment != null)
			{
				var mapControl = App.MainWindow.MapControl;
				mapControl.IsVisibilityCheckEnabled = !GameData.Data.User.IsSeeAll;
				mapControl.Environment = controllable.Environment;
				mapControl.AnimatedCenterPos = new System.Windows.Point(controllable.Location.X, controllable.Location.Y);
				mapControl.Z = controllable.Location.Z;
			}
		}

		void HandleMessage(LogOutReplyMessage msg)
		{
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
