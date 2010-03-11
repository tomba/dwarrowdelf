
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame.ClientMsgs;
using System.Runtime.Serialization;
using System.IO;
using System.ComponentModel;

namespace MyGame.Client
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
		Dictionary<Type, Action<Message>> m_handlerMap = new Dictionary<Type, Action<Message>>();
		int m_transactionNumber;

		public ClientNetStatistics Stats { get; private set; }

		public bool IsUserConnected { get; private set; }
		public bool IsCharConnected { get; private set; }

		Connection m_connection;

		public ClientConnection()
		{
			this.Stats = new ClientNetStatistics();
			m_connection = new Connection();
			m_connection.ReceiveEvent += ReceiveMessage;
			m_connection.DisconnectEvent += DisconnectOverride;
		}

		public void EnqueueAction(GameAction action)
		{
			int tid = System.Threading.Interlocked.Increment(ref m_transactionNumber);
			action.TransactionID = tid;
			Send(new EnqueueActionMessage() { Action = action });
		}

		public void Send(Message msg)
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
			app.Dispatcher.BeginInvoke(new Action<Message>(DeliverMessage), msg);
		}

		void DeliverMessage(Message msg)
		{
			this.Stats.ReceivedBytes = m_connection.ReceivedBytes;
			this.Stats.ReceivedMessages = m_connection.ReceivedMessages;
			this.Stats.Refresh();

			MyDebug.WriteLine("DeliverMessage {0}", msg);

			Action<Message> f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<Message>("HandleMessage", t, this);

				if (f == null)
					throw new Exception(String.Format("No msg handler for {0}", msg.GetType()));

				m_handlerMap[t] = f;
			}

			f(msg);
		}

		void DeliverMessages(IEnumerable<Message> messages)
		{
			foreach (Message msg in messages)
				DeliverMessage(msg);
		}

		public event Action LogOnEvent;

		void HandleMessage(LogOnReply msg)
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

		void HandleMessage(LogOffReply msg)
		{
			this.IsUserConnected = false;

			if (LogOffEvent != null)
				LogOffEvent();

			GameData.Data.World = null;
		}

		public event Action LogOnCharEvent;

		void HandleMessage(LogOnCharReply msg)
		{
			this.IsCharConnected = true;

			if (LogOnCharEvent != null)
				LogOnCharEvent();
		}

		void HandleMessage(ControllablesData msg)
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

		void HandleMessage(LogOffCharReply msg)
		{
			this.IsCharConnected = false;

			if (LogOffCharEvent != null)
				LogOffCharEvent();

			GameData.Data.World.Controllables.Clear();
			GameData.Data.CurrentObject = null;
			//App.MainWindow.FollowObject = null;
		}

		void HandleMessage(ObjectMove msg)
		{
			ClientGameObject ob = GameData.Data.World.FindObject<ClientGameObject>(msg.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object move from tile, that just case visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				return;
			}

			ClientGameObject env = null;
			if (msg.TargetEnvID != ObjectID.NullObjectID)
				env = GameData.Data.World.FindObject<ClientGameObject>(msg.TargetEnvID);

			ob.MoveTo(env, msg.TargetLocation);
		}

		void HandleMessage(MapData msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);

			if (env == null)
			{
				MyDebug.WriteLine("New map appeared {0}", msg.Environment);
				var world = GameData.Data.World;
				if (msg.Bounds.IsNull)
					env = new Environment(world, msg.Environment);
				else
					env = new Environment(world, msg.Environment, msg.Bounds);
				world.AddEnvironment(env);
				env.Name = "map";

				if (App.MainWindow.map.Environment == null)
					App.MainWindow.map.Environment = env;
			}

			env.VisibilityMode = msg.VisibilityMode;
		}

		void HandleMessage(MapDataTerrains msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			env.SetTerrains(msg.Bounds, msg.TerrainIDs);
		}

		void HandleMessage(MapDataTerrainsList msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			MyDebug.WriteLine("Received TerrainData for {0} tiles", msg.TileDataList.Count());
			env.SetTerrains(msg.TileDataList);
		}

		void HandleMessage(MapDataObjects msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			DeliverMessages(msg.ObjectData);
		}

		void HandleMessage(MapDataBuildings msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			env.SetBuildings(msg.BuildingData);
		}

		void HandleMessage(BuildingData msg)
		{
			var env = GameData.Data.World.FindObject<Environment>(msg.Environment);

			if (env.Buildings.Contains(msg.ObjectID))
			{
				//var building = env.Buildings[msg.ObjectID];
				throw new Exception();
			}
			else
			{
				var building = new BuildingObject(env.World, msg.ObjectID, msg.ID)
				{
					Area = msg.Area,
					Z = msg.Z,
					Environment = env,
				};

				env.AddBuilding(building);
			}
		}

		void HandleMessage(LivingData msg)
		{
			var ob = GameData.Data.World.FindObject<Living>(msg.ObjectID);

			if (ob == null)
			{
				MyDebug.WriteLine("New living appeared {0}/{1}", msg.Name, msg.ObjectID);
				ob = new Living(GameData.Data.World, msg.ObjectID);
			}

			ob.SymbolID = msg.SymbolID;
			ob.VisionRange = msg.VisionRange;
			ob.Name = msg.Name;
			ob.Color = msg.Color.ToColor();

			ClientGameObject env = null;
			if (msg.Environment != ObjectID.NullObjectID)
				env = GameData.Data.World.FindObject<ClientGameObject>(msg.Environment);

			ob.MoveTo(env, msg.Location);
		}

		void HandleMessage(PropertyData msg)
		{
			var ob = GameData.Data.World.FindObject<Living>(msg.ObjectID);

			if (ob == null)
				throw new Exception();

			switch (msg.PropertyID)
			{
				case PropertyID.HitPoints:
					ob.HitPoints = (int)msg.Value;
					break;

				case PropertyID.Strength:
					break;

				case PropertyID.VisionRange:
					ob.VisionRange = (int)msg.Value;
					break;

				case PropertyID.Color:
					ob.Color = ((GameColor)msg.Value).ToColor();
					break;

				default:
					throw new Exception();
			}
		}

		void HandleMessage(ItemData msg)
		{
			var ob = GameData.Data.World.FindObject<ItemObject>(msg.ObjectID);

			if (ob == null)
			{
				MyDebug.WriteLine("New object appeared {0}/{1}", msg.Name, msg.ObjectID);
				ob = new ItemObject(GameData.Data.World, msg.ObjectID);
			}

			ob.Deserialize(msg);
		}

		void HandleMessage(ObjectDestructedMessage msg)
		{
			var ob = GameData.Data.World.FindObject<ClientGameObject>(msg.ObjectID);

			ob.Destruct();
		}

		void HandleMessage(IronPythonOutput msg)
		{
			App.MainWindow.outputTextBox.AppendText(msg.Text);
			App.MainWindow.outputTextBox.ScrollToEnd();
		}

		void HandleMessage(EventMessage msg)
		{
			HandleEvents(msg.Event);
		}

		void HandleMessage(CompoundMessage msg)
		{
			DeliverMessages(msg.Messages);
		}

		void HandleEvents(Event @event)
		{
			if (@event is TickChangeEvent)
			{
				var e = (TickChangeEvent)@event;
				GameData.Data.World.TickNumber = e.TickNumber;
			}
			else if (@event is ActionProgressEvent)
			{
				var e = (ActionProgressEvent)@event;

				//MyDebug.WriteLine("ActionProgressEvent({0})", e.TransactionID);

				var list = GameData.Data.ActionCollection;
				GameAction action = list.SingleOrDefault(a => a.TransactionID == e.TransactionID);
				action.TicksLeft = e.TicksLeft;

				// XXX GameAction doesn't have INotifyProperty changed, so we have to update manually
				var itemsView = System.Windows.Data.CollectionViewSource.GetDefaultView(App.MainWindow.actionList.ItemsSource);
				itemsView.Refresh();

				var ob = GameData.Data.World.FindObject<Living>(action.ActorObjectID);
				if (ob == null)
					throw new Exception();

				if (e.TicksLeft == 0)
					ob.ActionDone(action);

				ob.AI.ActionProgress(e);
			}
			else if (@event is ActionRequiredEvent)
			{
				var e = (ActionRequiredEvent)@event;

				//MyDebug.WriteLine("{0}", e);

				var ob = GameData.Data.World.FindObject<Living>(e.ObjectID);

				if (ob == null)
					throw new Exception();

				ob.AI.ActionRequired();
			}
		}
	}
}
