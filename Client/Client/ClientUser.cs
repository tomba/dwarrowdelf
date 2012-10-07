using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Messages;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;

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

		public enum ClientUserState
		{
			None,
			Connecting,
			LoggingIn,
			ReceivingLoginData,
			LoggedIn,
		}

		ClientUserState m_state;
		public ClientUserState State
		{
			get { return m_state; }

			private set
			{
				m_state = value;

				if (this.StateChangedEvent != null)
				{
					if (App.Current.Dispatcher.CheckAccess())
						this.StateChangedEvent(m_state);
					else
						App.Current.Dispatcher.BeginInvoke(this.StateChangedEvent, m_state);
				}
			}
		}

		public event Action<ClientUserState> StateChangedEvent;

		public event Action DisconnectEvent;

		ReportHandler m_reportHandler;
		ChangeHandler m_changeHandler;

		IConnection m_connection;

		public bool IsSeeAll { get; private set; }

		World m_world;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection", "ClientUser");

		public ClientNetStatistics NetStats { get; private set; }

		public ClientUser()
		{
			this.NetStats = new ClientNetStatistics();
			this.State = ClientUserState.None;
		}

		// XXX add cancellationtoken
		public async Task LogOn(string name)
		{
			this.State = ClientUserState.Connecting;

			switch (ClientConfig.ConnectionType)
			{
				case ConnectionType.Tcp:
					m_connection = await TcpConnection.ConnectAsync();
					break;

				case ConnectionType.Direct:
					var server = GameData.Data.ConnectManager.Server;
					m_connection = DirectConnection.Connect(server.Game);
					break;

				case ConnectionType.Pipe:
					m_connection = PipeConnection.Connect();
					break;

				default:
					throw new Exception();
			}

			this.State = ClientUserState.LoggingIn;

			m_connection.NewMessageEvent += _OnNewMessages;
			m_opEvent = new ManualResetEvent(false);

			var tcs = new TaskCompletionSource<object>();

			ThreadPool.RegisterWaitForSingleObject(m_opEvent,
				(o, tout) =>
				{
					if (tout)
						tcs.SetException(new Exception("timeout"));
					else
						tcs.SetResult(null);
				},
				null, TimeSpan.FromSeconds(60), true);

			Send(new Messages.LogOnRequestMessage() { Name = name });

			await tcs.Task;

			m_opEvent.Dispose();
			m_opEvent = null;

			this.State = ClientUserState.LoggedIn;
		}

		ManualResetEvent m_opEvent;

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

			this.NetStats.SentBytes = m_connection.SentBytes;
			this.NetStats.SentMessages = m_connection.SentMessages;
			this.NetStats.AddSentMessages(msg);
		}

		public void SendLogOut()
		{
			Send(new Messages.LogOutRequestMessage());
		}

		volatile bool m_onNewMessagesInvoked;

		void _OnNewMessages()
		{
			if (m_onNewMessagesInvoked)
				return;

			m_onNewMessagesInvoked = true;

			Application.Current.Dispatcher.BeginInvoke(new Action(OnNewMessages));
		}

		void OnNewMessages()
		{
			Message msg;

			while (m_connection.TryGetMessage(out msg))
				OnReceiveMessage((ClientMessage)msg);

			m_onNewMessagesInvoked = false;

			while (m_connection.TryGetMessage(out msg))
				OnReceiveMessage((ClientMessage)msg);

			if (m_connection.IsConnected == false)
				OnDisconnected();
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnect");

			GameData.Data.Jobs.Clear();
			GameData.Data.World = null;

			if (DisconnectEvent != null)
				DisconnectEvent();
		}

		public void OnReceiveMessage(ClientMessage msg)
		{
			//trace.TraceVerbose("Received Message {0}", msg);

			this.NetStats.ReceivedBytes = m_connection.ReceivedBytes;
			this.NetStats.ReceivedMessages = m_connection.ReceivedMessages;
			this.NetStats.AddReceivedMessages(msg);

			var method = s_handlerMap[msg.GetType()];
			method(this, msg);
		}

		void HandleMessage(LogOnReplyBeginMessage msg)
		{
			this.State = ClientUserState.ReceivingLoginData;

			m_world = new World(msg.GameMode, msg.LivingVisionMode, msg.Tick);

			m_reportHandler = new ReportHandler(m_world);
			m_changeHandler = new ChangeHandler(m_world);
			m_changeHandler.TurnEnded += OnTurnEnded;

			this.IsSeeAll = msg.IsSeeAll;

			GameData.Data.World = m_world;
		}

		void HandleMessage(LogOnReplyEndMessage msg)
		{
			if (msg.ClientData != null)
				ClientSaveManager.Load(msg.ClientData);

			if (m_opEvent != null)
				m_opEvent.Set();
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
			env.SetTerrains(msg.Bounds, msg.TerrainData, msg.IsTerrainDataCompressed);
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


		MyTraceSource turnTrace = new MyTraceSource("Dwarrowdelf.Turn", "ClientUser");

		// LivingID, AnyObjectID or NullIObjectID
		ObjectID m_currentLivingID;
		Dictionary<LivingObject, GameAction> m_actionMap = new Dictionary<LivingObject, GameAction>();

		// Called from change handler
		void OnTurnEnded()
		{
			turnTrace.TraceInformation("TurnEnd");
			m_currentLivingID = ObjectID.NullObjectID;
		}

		void TurnActionRequested(ObjectID livingID)
		{
			turnTrace.TraceInformation("Turn Action requested for living: {0}", livingID);

			Debug.Assert(livingID != ObjectID.NullObjectID);
			Debug.Assert(m_currentLivingID == ObjectID.NullObjectID);

			m_currentLivingID = livingID;

			if (GameData.Data.IsAutoAdvanceTurn)
				SendProceedTurn();
		}

		public void SignalLivingHasAction(LivingObject living, GameAction action)
		{
			turnTrace.TraceInformation("SignalLivingHasAction({0}, {1}", living, action);

			if (m_currentLivingID == ObjectID.NullObjectID)
				return;

			if (m_currentLivingID != ObjectID.AnyObjectID && living.ObjectID != m_currentLivingID)
				throw new Exception();

			m_actionMap[living] = action;

			SendProceedTurn();
		}

		public void SendProceedTurn()
		{
			turnTrace.TraceInformation("SendProceedTurn");

			if (m_currentLivingID == ObjectID.NullObjectID)
				return;

			// livings which the user can control (ie. server not doing high priority action)
			var livings = m_world.Controllables.Where(l => l.UserActionPossible());
			var list = new List<Tuple<ObjectID, GameAction>>();
			foreach (var living in livings)
			{
				GameAction action;

				if (m_actionMap.TryGetValue(living, out action) == false)
					action = living.DecideAction();

				Debug.Assert(action == null || action.MagicNumber != 0);

				if (action != living.CurrentAction)
				{
					turnTrace.TraceInformation("{0}: selecting new action {1}", living, action);
					list.Add(new Tuple<ObjectID, GameAction>(living.ObjectID, action));
				}
			}

			Send(new ProceedTurnReplyMessage() { Actions = list.ToArray() });

			m_currentLivingID = ObjectID.NullObjectID;
			m_actionMap.Clear();
		}
	}
}
