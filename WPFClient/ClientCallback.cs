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
			GameData.Data.UserID = userID;
			GameData.Data.Connection.Server.LogOnChar("tomba");
		}

		void _LogOnCharReply(ObjectID playerID)
		{
			ClientGameObject player = new ClientGameObject(playerID);
			GameData.Data.Controllables.Add(player);
			if (GameData.Data.CurrentObject == null)
				GameData.Data.CurrentObject = player;

			if (App.MainWindow.FollowObject == null)
				App.MainWindow.FollowObject = player;
			else if (App.MainWindow.MiniMap.FollowObject == null)
				App.MainWindow.MiniMap.FollowObject = player;
		}

		void _LogOffCharReply()
		{
			GameData.Data.Controllables.Clear();
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
				var env = ClientGameObject.FindObject<Environment>(td.Environment);
				if (env == null)
					throw new Exception();
				env.SetTerrains(td.MapDataList);
			}
			else if (msg is ObjectMove)
			{
				ObjectMove om = (ObjectMove)msg;
				ClientGameObject ob = ClientGameObject.FindObject(om.ObjectID);

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
					env = ClientGameObject.FindObject(om.TargetEnvID);

				ob.MoveTo(env, om.TargetLocation);
			}
			else if (msg is FullMapData)
			{
				FullMapData md = (FullMapData)msg;

				var env = ClientGameObject.FindObject<Environment>(md.ObjectID);

				if (env == null)
				{
					MyDebug.WriteLine("New map appeared {0}", md.ObjectID);
					var world = World.TheWorld;
					env = new Environment(world, md.ObjectID, md.Bounds);
					world.AddEnvironment(env);
					env.Name = "map";
				}

				env.SetTerrains(md.Bounds, md.TerrainIDs);

				_DeliverMessages(md.ObjectData);
			}
			else if (msg is MapData)
			{
				MapData md = (MapData)msg;

				var env = ClientGameObject.FindObject<Environment>(md.ObjectID);

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

				ClientGameObject ob = ClientGameObject.FindObject(ld.ObjectID);

				if (ob == null)
				{
					MyDebug.WriteLine("New living appeared {0}", ld.ObjectID);
					ob = new ClientGameObject(ld.ObjectID);
				}

				ob.SymbolID = ld.SymbolID;
				ob.VisionRange = ld.VisionRange;
				ob.Name = ld.Name;
				ob.Color = ld.Color.ToColor();
				ob.IsLiving = true;

				ClientGameObject env = null;
				if (ld.Environment != ObjectID.NullObjectID)
					env = ClientGameObject.FindObject(ld.Environment);

				ob.MoveTo(env, ld.Location);
			}
			else if (msg is ItemData)
			{
				ItemData id = (ItemData)msg;

				var ob = ClientGameObject.FindObject<ItemObject>(id.ObjectID);

				if (ob == null)
				{
					MyDebug.WriteLine("New object appeared {0}", id.ObjectID);
					ob = new ItemObject(id.ObjectID);
				}

				ob.Name = id.Name;
				ob.SymbolID = id.SymbolID;
				ob.Color = id.Color.ToColor();
				ob.IsLiving = false;

				ClientGameObject env = null;
				if (id.Environment != ObjectID.NullObjectID)
					env = ClientGameObject.FindObject(id.Environment);

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
				GameData.Data.TurnNumber = e.TurnNumber;
			}
			else if (@event is ActionDoneEvent)
			{
				var e = (ActionDoneEvent)@event;

				MyDebug.WriteLine("TransactionDone({0})", e.TransactionID);
				GameAction action = GameData.Data.ActionCollection.SingleOrDefault(a => a.TransactionID == e.TransactionID);
				GameData.Data.ActionCollection.Remove(action);
			}
		}
	}
}
