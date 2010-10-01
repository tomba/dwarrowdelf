
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Dwarrowdelf.Messages;
using System.Runtime.Serialization;
using System.IO;
using System.ComponentModel;

using Dwarrowdelf;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class ClientNetStatistics : INotifyPropertyChanged
	{
		public int SentMessages { get; set; }
		public int SentBytes { get; set; }
		public int ReceivedMessages { get; set; }
		public int ReceivedBytes { get; set; }

		public void Refresh()
		{
			Notify("SentMessages");
			Notify("SentBytes");
			Notify("ReceivedMessages");
			Notify("ReceivedBytes");
		}

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	class ClientConnection
	{
		Dictionary<Type, Action<ServerMessage>> m_handlerMap = new Dictionary<Type, Action<ServerMessage>>();
		Dictionary<Type, Action<Change>> m_changeHandlerMap = new Dictionary<Type, Action<Change>>();
		public ClientNetStatistics Stats { get; private set; }

		public bool IsUserConnected { get; private set; }
		public bool IsCharConnected { get; private set; }

		IConnection m_connection;

		public ClientConnection()
		{
			this.Stats = new ClientNetStatistics();
			m_connection = new Connection();
			m_connection.ReceiveEvent += ReceiveMessage;
			m_connection.DisconnectEvent += DisconnectOverride;
		}

		public void Send(ClientMessage msg)
		{
			m_connection.Send(msg);

			this.Stats.SentBytes = m_connection.SentBytes;
			this.Stats.SentMessages = m_connection.SentMessages;
			this.Stats.Refresh();
		}

		public void Disconnect()
		{
			m_connection.Disconnect();
		}

		protected void DisconnectOverride()
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action(ServerDisconnected));
		}

		void ServerDisconnected()
		{
			this.IsCharConnected = false;
			this.IsUserConnected = false;

			GameData.Data.World = null;
		}

		public void BeginConnect(Action callback)
		{
			m_connection.BeginConnect(callback);
		}

		protected void ReceiveMessage(Message msg)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<ServerMessage>(DeliverMessage), msg);
		}

		void DeliverMessage(ServerMessage msg)
		{
			this.Stats.ReceivedBytes = m_connection.ReceivedBytes;
			this.Stats.ReceivedMessages = m_connection.ReceivedMessages;
			this.Stats.Refresh();

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

		public event Action LogOnEvent;

		void HandleMessage(LogOnReplyMessage msg)
		{
			this.IsUserConnected = true;

			GameData.Data.IsSeeAll = msg.IsSeeAll;

			var world = new World();
			GameData.Data.World = world;

			GameData.Data.World.UserID = msg.UserID;

			if (LogOnEvent != null)
				LogOnEvent();
		}

		public event Action LogOffEvent;

		void HandleMessage(LogOffReplyMessage msg)
		{
			this.IsUserConnected = false;

			if (LogOffEvent != null)
				LogOffEvent();

			GameData.Data.World = null;
		}

		public event Action LogOnCharEvent;

		void HandleMessage(LogOnCharReplyMessage msg)
		{
			this.IsCharConnected = true;

			if (LogOnCharEvent != null)
				LogOnCharEvent();
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

		public event Action LogOffCharEvent;

		void HandleMessage(LogOffCharReplyMessage msg)
		{
			this.IsCharConnected = false;

			if (LogOffCharEvent != null)
				LogOffCharEvent();

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
				Debug.Print("New gameobject appeared {0}", data.ObjectID);

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
		void HandleMessage(MapDataMessage msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);

			if (env == null)
			{
				Debug.Print("New map appeared {0}", msg.Environment);
				var world = GameData.Data.World;
				if (msg.Bounds.IsNull)
					env = new Environment(world, msg.Environment);
				else
					env = new Environment(world, msg.Environment, msg.Bounds);
				env.Name = "map";
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
					new Environment(world, change.ObjectID);
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
			var env = GameData.Data.World.FindObject<Environment>(change.MapID);
			if (env == null)
				throw new Exception();
			env.SetTileData(change.Location, change.TileData);
		}

		void HandleChange(TickStartChange change)
		{
			GameData.Data.World.TickNumber = change.TickNumber;
		}

		void HandleChange(TurnStartChange change)
		{
			Debug.Assert(m_turnActionRequested == false);

			m_turnActionRequested = true;

			if (change.LivingID == ObjectID.NullObjectID)
			{
				m_numActionsGot = 0;

				var livings = GameData.Data.World.Controllables
					.Where(l => l.UserActionPossible());

				m_actionMap.Clear();
				foreach (var living in livings)
					m_actionMap[living] = null;

				if (GameData.Data.IsAutoAdvanceTurn)
					SendProceedTurn();
			}
			else
			{
				var living = GameData.Data.World.FindObject<Living>(change.LivingID);
				if (living == null)
					throw new Exception();
				throw new Exception();
			}
		}

		bool m_turnActionRequested;
		int m_numActionsGot;
		Dictionary<Living, GameAction> m_actionMap = new Dictionary<Living, GameAction>();

		public void SignalLivingHasAction(Living living, GameAction action)
		{
			if (m_turnActionRequested == false)
				return;

			if (!m_actionMap.ContainsKey(living))
				throw new Exception();

			m_actionMap[living] = action;
			m_numActionsGot++;

			if (GameData.Data.IsAutoAdvanceTurn)
				CheckProceedTurn();
		}

		void CheckProceedTurn()
		{
			if (m_turnActionRequested == false)
				return;

			if (m_numActionsGot < m_actionMap.Count)
				return;

			SendProceedTurn();
		}

		public void SendProceedTurn()
		{
			if (m_turnActionRequested == false)
				return;

			foreach (var living in m_actionMap.Keys.ToArray())
			{
				var action = living.DecideAction(ActionPriority.Normal);
				m_actionMap[living] = action;
			}

			var actions = m_actionMap.Select(kvp => new Tuple<ObjectID, GameAction>(kvp.Key.ObjectID, kvp.Value)).ToArray();
			Send(new ProceedTurnMessage() { Actions = actions });
			m_actionMap.Clear();
			m_numActionsGot = 0;
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

			ob.ActionProgress(change);
		}
	}
}
