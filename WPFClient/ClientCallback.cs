using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using MyGame.ClientMsgs;

namespace MyGame
{
	class ClientCallback : IClientCallback
	{
		public ClientCallback()
		{
		}

		#region IClientCallback Members

		public void LogOnReply(int userID)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<int>(_LogOnReply), userID);
		}

		public void LogOnCharReply(ObjectID playerID)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<ObjectID>(_LogOnCharReply), playerID);
		}

		public void LogOffCharReply()
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action(_LogOffCharReply));
		}

		public void DeliverMessage(Message msg)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<Message>(_DeliverMessage), msg);
		}

		public void DeliverMessages(IEnumerable<Message> messages)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<IEnumerable<Message>>(_DeliverMessages), messages);
		}

		#endregion

		void _LogOnReply(int userID)
		{
			World.TheWorld.UserID = userID;
			GameData.Data.Connection.Server.LogOnChar("tomba");
		}

		void _LogOnCharReply(ObjectID playerID)
		{
			ClientGameObject player = new ClientGameObject(World.TheWorld, playerID);
			World.TheWorld.Controllables.Add(player);
			if (GameData.Data.CurrentObject == null)
				GameData.Data.CurrentObject = player;

			if (App.MainWindow.FollowObject == null)
				App.MainWindow.FollowObject = player;
			else if (App.MainWindow.MiniMap.FollowObject == null)
				App.MainWindow.MiniMap.FollowObject = player;
		}

		void _LogOffCharReply()
		{
			World.TheWorld.Controllables.Clear();
			GameData.Data.CurrentObject = null;
			App.MainWindow.FollowObject = null;
			App.MainWindow.MiniMap.FollowObject = null;
		}

		void _DeliverMessage(Message msg)
		{
			MyDebug.WriteLine("Received msg {0}", msg);

			if (msg is TerrainData)
			{
				TerrainData td = (TerrainData)msg;
				var env = World.TheWorld.FindObject<Environment>(td.Environment);
				if (env == null)
					throw new Exception();
				MyDebug.WriteLine("Received TerrainData for {0} tiles", td.MapDataList.Count());
				env.SetTerrains(td.MapDataList);
			}
			else if (msg is ObjectMove)
			{
				ObjectMove om = (ObjectMove)msg;
				ClientGameObject ob = World.TheWorld.FindObject(om.ObjectID);

				if (ob == null)
				{
					/* There's a special case where we don't get objectinfo, but we do get
					 * ObjectMove: If the object move from tile, that just case visible to us, 
					 * to a tile that we cannot see. So let's not throw exception, but exit
					 * silently */
					return;
				}

				ClientGameObject env = null;
				if (om.TargetEnvID != ObjectID.NullObjectID)
					env = World.TheWorld.FindObject(om.TargetEnvID);

				ob.MoveTo(env, om.TargetLocation);
			}
			else if (msg is FullMapData)
			{
				FullMapData md = (FullMapData)msg;

				var env = World.TheWorld.FindObject<Environment>(md.ObjectID);

				if (env == null)
				{
					MyDebug.WriteLine("New map appeared {0}", md.ObjectID);
					var world = World.TheWorld;
					env = new Environment(world, md.ObjectID, md.Bounds);
					world.AddEnvironment(env);
					env.Name = "map";
				}

				MyDebug.WriteLine("Received TerrainData for {0} tiles", md.TerrainIDs.Count());
				env.SetTerrains(md.Bounds, md.TerrainIDs);
				env.VisibilityMode = md.VisibilityMode;

				_DeliverMessages(md.ObjectData);
			}
			else if (msg is MapData)
			{
				MapData md = (MapData)msg;

				var env = World.TheWorld.FindObject<Environment>(md.ObjectID);

				if (env == null)
				{
					MyDebug.WriteLine("New map appeared {0}", md.ObjectID);
					var world = World.TheWorld;
					env = new Environment(world, md.ObjectID);
					world.AddEnvironment(env);
					env.Name = "map";
				}

				env.VisibilityMode = md.VisibilityMode;
			}
			else if (msg is LivingData)
			{
				LivingData ld = (LivingData)msg;

				ClientGameObject ob = World.TheWorld.FindObject(ld.ObjectID);

				if (ob == null)
				{
					MyDebug.WriteLine("New living appeared {0}", ld.ObjectID);
					ob = new ClientGameObject(World.TheWorld, ld.ObjectID);
				}

				ob.SymbolID = ld.SymbolID;
				ob.VisionRange = ld.VisionRange;
				ob.Name = ld.Name;
				ob.Color = ld.Color.ToColor();
				ob.IsLiving = true;

				ClientGameObject env = null;
				if (ld.Environment != ObjectID.NullObjectID)
					env = World.TheWorld.FindObject(ld.Environment);

				ob.MoveTo(env, ld.Location);
			}
			else if (msg is ItemData)
			{
				ItemData id = (ItemData)msg;

				var ob = World.TheWorld.FindObject<ItemObject>(id.ObjectID);

				if (ob == null)
				{
					MyDebug.WriteLine("New object appeared {0}", id.ObjectID);
					ob = new ItemObject(World.TheWorld, id.ObjectID);
				}

				ob.Name = id.Name;
				ob.SymbolID = id.SymbolID;
				ob.Color = id.Color.ToColor();
				ob.IsLiving = false;

				ClientGameObject env = null;
				if (id.Environment != ObjectID.NullObjectID)
					env = World.TheWorld.FindObject(id.Environment);

				ob.MoveTo(env, id.Location);
			}
			else if (msg is EventMessage)
			{
				HandleEvents(((EventMessage)msg).Event);
			}
			else if (msg is CompoundMessage)
			{
				_DeliverMessages(((CompoundMessage)msg).Messages);
			}
			else
			{
				throw new Exception("unknown messagetype");
			}
		}

		void _DeliverMessages(IEnumerable<Message> messages)
		{
			foreach (Message msg in messages)
				_DeliverMessage(msg);
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

				MyDebug.WriteLine("ActionProgressEvent({0})", e.TransactionID);

				var list = GameData.Data.ActionCollection;
				GameAction action = list.SingleOrDefault(a => a.TransactionID == e.TransactionID);
				action.TurnsLeft = e.TurnsLeft;

				// XXX GameAction doesn't have INotifyProperty changed, so we have to update manually
				var itemsView = System.Windows.Data.CollectionViewSource.GetDefaultView(App.MainWindow.actionList.ItemsSource);
				itemsView.Refresh();

				if (e.TurnsLeft == 0)
					GameData.Data.ActionCollection.Remove(action);
			}
			else if (@event is ActionRequiredEvent)
			{
				var e = (ActionRequiredEvent)@event;

				MyDebug.WriteLine("{0}", e);

				var ob = World.TheWorld.FindObject(e.ObjectID);

				if (ob == null)
					throw new Exception();

				GameData.Data.CurrentObject = ob;

			}
		}
	}
}
