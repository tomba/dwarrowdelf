
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

namespace MyGame
{
	public class ClientNetStatistics : INotifyPropertyChanged
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

	public class ClientConnection : Connection
	{
		Dictionary<Type, Action<Message>> m_handlerMap = new Dictionary<Type, Action<Message>>();
		int m_transactionNumber;

		public ClientNetStatistics Stats { get; private set; }

		public ClientConnection() : base()
		{
			this.Stats = new ClientNetStatistics();
		}

		public void EnqueueAction(GameAction action)
		{
			int tid = System.Threading.Interlocked.Increment(ref m_transactionNumber);
			action.TransactionID = tid;
			Send(new EnqueueActionMessage() { Action = action });
		}

		public override void Send(Message msg)
		{
			base.Send(msg);

			this.Stats.SentBytes = base.SentBytes;
			this.Stats.SentMessages = base.SentMessages;
			this.Stats.Refresh();
		}

		protected override void ReceiveMessage(Message msg)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<Message>(DeliverMessage), msg);
		}

		void DeliverMessage(Message msg)
		{
			this.Stats.ReceivedBytes = base.ReceivedBytes;
			this.Stats.ReceivedMessages = base.ReceivedMessages;
			this.Stats.Refresh();

			MyDebug.WriteLine("[RX] {0}", msg);

			Action<Message> f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<Message>("HandleMessage", t, this);

				if (f == null)
					throw new Exception();

				m_handlerMap[t] = f;
			}

			f(msg);
		}

		void DeliverMessages(IEnumerable<Message> messages)
		{
			foreach (Message msg in messages)
				DeliverMessage(msg);
		}

		void HandleMessage(LogOnReply msg)
		{
			World.TheWorld.UserID = msg.UserID;
			GameData.Data.Connection.Send(new LogOnCharRequest() { Name = "tomba" });
		}

		void HandleMessage(LogOnCharReply msg)
		{
			var player = new Living(World.TheWorld, msg.PlayerID);
			World.TheWorld.Controllables.Add(player);
			if (GameData.Data.CurrentObject == null)
				GameData.Data.CurrentObject = player;

			if (App.MainWindow.FollowObject == null)
				App.MainWindow.FollowObject = player;
			else if (App.MainWindow.MiniMap.FollowObject == null)
				App.MainWindow.MiniMap.FollowObject = player;
		}

		void HandleMessage(LogOffCharReply msg)
		{
			World.TheWorld.Controllables.Clear();
			GameData.Data.CurrentObject = null;
			App.MainWindow.FollowObject = null;
			App.MainWindow.MiniMap.FollowObject = null;
		}

		void HandleMessage(TerrainData msg)
		{
			var env = World.TheWorld.FindObject<Environment>(msg.Environment);
			if (env == null)
				throw new Exception();
			MyDebug.WriteLine("Received TerrainData for {0} tiles", msg.TileDataList.Count());
			env.SetTerrains(msg.TileDataList);
		}

		void HandleMessage(ObjectMove msg)
		{
			ClientGameObject ob = World.TheWorld.FindObject(msg.ObjectID);

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
				env = World.TheWorld.FindObject(msg.TargetEnvID);

			ob.MoveTo(env, msg.TargetLocation);
		}

		void HandleMessage(FullMapData msg)
		{
			var env = World.TheWorld.FindObject<Environment>(msg.ObjectID);

			if (env == null)
			{
				MyDebug.WriteLine("New map appeared {0}", msg.ObjectID);
				var world = World.TheWorld;
				env = new Environment(world, msg.ObjectID, msg.Bounds);
				world.AddEnvironment(env);
				env.Name = "map";
			}

			MyDebug.WriteLine("Received TerrainData for {0} tiles", msg.TerrainIDs.Count());
			env.SetTerrains(msg.Bounds, msg.TerrainIDs);
			env.VisibilityMode = msg.VisibilityMode;

			DeliverMessages(msg.ObjectData);
		}

		void HandleMessage(MapData msg)
		{
			var env = World.TheWorld.FindObject<Environment>(msg.Environment);

			if (env == null)
			{
				MyDebug.WriteLine("New map appeared {0}", msg.Environment);
				var world = World.TheWorld;
				env = new Environment(world, msg.Environment);
				world.AddEnvironment(env);
				env.Name = "map";
			}

			env.VisibilityMode = msg.VisibilityMode;
		}

		void HandleMessage(LivingData msg)
		{
			var ob = World.TheWorld.FindObject<Living>(msg.ObjectID);

			if (ob == null)
			{
				MyDebug.WriteLine("New living appeared {0}/{1}", msg.Name, msg.ObjectID);
				ob = new Living(World.TheWorld, msg.ObjectID);
			}

			ob.SymbolID = msg.SymbolID;
			ob.VisionRange = msg.VisionRange;
			ob.Name = msg.Name;
			ob.Color = msg.Color.ToColor();

			ClientGameObject env = null;
			if (msg.Environment != ObjectID.NullObjectID)
				env = World.TheWorld.FindObject(msg.Environment);

			ob.MoveTo(env, msg.Location);
		}

		void HandleMessage(ItemData msg)
		{
			var ob = World.TheWorld.FindObject<ItemObject>(msg.ObjectID);

			if (ob == null)
			{
				MyDebug.WriteLine("New object appeared {0}/{1}", msg.Name, msg.ObjectID);
				ob = new ItemObject(World.TheWorld, msg.ObjectID);
			}

			ob.Deserialize(msg);
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
			if (@event is TurnChangeEvent)
			{
				var e = (TurnChangeEvent)@event;
				World.TheWorld.TurnNumber = e.TurnNumber;
			}
			else if (@event is ActionProgressEvent)
			{
				var e = (ActionProgressEvent)@event;

				//MyDebug.WriteLine("ActionProgressEvent({0})", e.TransactionID);

				var list = GameData.Data.ActionCollection;
				GameAction action = list.SingleOrDefault(a => a.TransactionID == e.TransactionID);
				action.TurnsLeft = e.TurnsLeft;

				// XXX GameAction doesn't have INotifyProperty changed, so we have to update manually
				var itemsView = System.Windows.Data.CollectionViewSource.GetDefaultView(App.MainWindow.actionList.ItemsSource);
				itemsView.Refresh();

				var ob = World.TheWorld.FindObject<Living>(action.ActorObjectID);
				if (ob == null)
					throw new Exception();

				if (e.TurnsLeft == 0)
					ob.ActionDone(action);

				ob.AI.ActionProgress(e);
			}
			else if (@event is ActionRequiredEvent)
			{
				var e = (ActionRequiredEvent)@event;

				//MyDebug.WriteLine("{0}", e);

				var ob = World.TheWorld.FindObject<Living>(e.ObjectID);

				if (ob == null)
					throw new Exception();

				ob.AI.ActionRequired();
			}
		}
	}
}
