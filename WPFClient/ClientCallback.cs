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

		public void LoginReply(ObjectID playerID)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<ObjectID>(_LoginReply), playerID);
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

		public void TransactionDone(int transactionID)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<int>(_TransactionDone), transactionID);
		}

		#endregion


		void _LoginReply(ObjectID playerID)
		{
			ClientGameObject player = new ClientGameObject(playerID);
			GameData.Data.Player = player;
			MainWindow.s_mainWindow.map.FollowObject = player;
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
			else if (msg is ClientMsgs.TurnChange)
			{
				GameData.Data.TurnNumber = ((ClientMsgs.TurnChange)msg).TurnNumber;
			}
			else if (msg is ObjectMove)
			{
				ObjectMove om = (ObjectMove)msg;
				ClientGameObject ob = ClientGameObject.FindObject(om.ObjectID);
				
				if (ob == null)
				{
					/* An object moved into our field of vision, but we didn't get
					 * the object info yet. It should be coming just after the move
					 * changes, so we'll just skip this.
					 * I'm not sure if this is good... */
					return;
				}

				ClientGameObject env = null;
				if (om.TargetEnvID != ObjectID.NullObjectID)
					env = ClientGameObject.FindObject(om.TargetEnvID);

				ob.MoveTo(env, om.TargetLocation);
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
					MainWindow.s_mainWindow.Map = env;
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

		void _TransactionDone(int transactionID)
		{
			MyDebug.WriteLine("TransactionDone({0})", transactionID);
			GameAction action = GameData.Data.ActionCollection.SingleOrDefault(a => a.TransactionID == transactionID);
			GameData.Data.ActionCollection.Remove(action);
		}
	}
}
